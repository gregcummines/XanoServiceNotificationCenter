﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;

namespace XanoSNCLibrary
{
    /// <summary>
    /// Starkey WCF service to handle notifications and subscriptions between Xano subsystems
    /// Example: 
    /// 1. Roka wants to send a FirmwareRelease notification to subsystems, so Roka creates the 
    ///    FirmwareRelease notification type.
    /// 2. PSM gets notifications and finds that there is a FirmwareRelease notification type.
    ///    It is not necessary, but PSM could check the notification and find that Roka owns it. 
    /// 3. PSM subscribes to the FirmwareRelease notification and gives the SNC a Url to call. 
    /// 4. Roka handles a FirmwareRelease and notifies all subscribers (PSM is subscribed)
    /// 5. PSM wants to temporarily not listen to the FirmwareRelease notification, so PSM
    ///    unsubscribes from that notification. 
    /// </summary>
    public class XanoSNCService : IXanoSNCService
    {
        // Greg 10/25/2015: I wanted to dependency inject the repository interface into the 
        // WCF service constructor using an IOC framework, such as Unity, ServiceMap, 
        // Castlerock, or Ninject. But I can't figure out how to do the registration before this 
        // object is instantiated. So I built a singleton for the repository until this can be 
        // figured out. The downside is that I cannot unit test this component without using 
        // the repository singleton!!! 
        //private IXanoSNCRepository SNCRepository;

        /// <summary>
        /// Used to create a notification. Any service can create a notification and notify
        /// any subscribers of changes. 
        /// </summary>
        /// <param name="request"></param>
        public string CreateNotificationEvent(string publisher, string notificationEvent, Stream jsonSchema)
        {
            if (publisher == null)
                ThrowWebFault("publisher is null", HttpStatusCode.BadRequest);
            if (notificationEvent == null)
                ThrowWebFault("notificationEvent is null", HttpStatusCode.BadRequest);
            if (jsonSchema == null)
                ThrowWebFault("jsonSchema is null", HttpStatusCode.BadRequest);

            string jsonSchemaString = string.Empty;
            string token = string.Empty;
            try
            {
                using (var reader = new StreamReader(jsonSchema))
                {
                    jsonSchemaString = reader.ReadToEnd();
                }
            }
            catch(Exception e)
            {
                ThrowWebFault(e.Message, HttpStatusCode.InternalServerError);
            }

            try
            {
                token = XanoSNCRepository.Instance.CreateNotificationEvent(publisher, notificationEvent, jsonSchemaString);
            }
            catch(Exception e)
            {
                ThrowWebFaultOnRepositoryException(e);
            }
            return token;
        }

        /// <summary>
        /// Gets the list of notifications supported by the XanoSNC
        /// </summary>
        /// <returns></returns>
        public List<string> GetNotificationEvents()
        {
            try
            {
                return XanoSNCRepository.Instance.GetNotificationEvents();
            }
            catch(Exception e)
            {
                ThrowWebFaultOnRepositoryException(e);
            }
            return null;
        }

        public Stream GetNotificationEventMessageSchema(string notificationEvent)
        {
            var jsonSchemaString = string.Empty;
            try
            {
                jsonSchemaString = XanoSNCRepository.Instance.GetNotificationEventMessageSchema(notificationEvent);
            }
            catch (Exception e)
            {
                ThrowWebFaultOnRepositoryException(e);
            }

            WebOperationContext.Current.OutgoingResponse.ContentType =
                "application/json; charset=utf-8";
            return new MemoryStream(Encoding.UTF8.GetBytes(jsonSchemaString));
        }

        public string Subscribe(string subscriber, string notification, string notifyUrl)
        {
            try
            {
                return XanoSNCRepository.Instance.Subscribe(subscriber, notification, notifyUrl);
            }
            catch (Exception e)
            {
                ThrowWebFaultOnRepositoryException(e);
            }
            return null;
        }

        public void Unsubscribe(string subscriber, string notificationEvent, string token)
        {
            try
            {
                XanoSNCRepository.Instance.Unsubscribe(subscriber, notificationEvent, token);
            }
            catch (Exception e)
            {
                ThrowWebFaultOnRepositoryException(e);
            }
        }

        /// <summary>
        /// Method used to allow a service to send a notification on the notification
        /// </summary>
        /// <param name="publisher">publisher that published the notification event</param>
        /// <param name="notificationEvent">notification event being published</param>
        /// <param name="token">Token required to verify that you are the publisher of this notification event</param>
        /// <param name="json">json object with more information about the notification event</param>
        public void NotifySubscribers(string publisher, string notificationEvent, string token, string json)
        {
            if (publisher == null)
                ThrowWebFault("publisher is null", HttpStatusCode.BadRequest);
            if (notificationEvent == null)
                ThrowWebFault("notificationEvent is null", HttpStatusCode.BadRequest);
            if (token == null)
                ThrowWebFault("token is null", HttpStatusCode.BadRequest);
            if (json == null)
                ThrowWebFault("json is null", HttpStatusCode.BadRequest);

            int notificationId = 0;
            try
            {
                // Verify that this is the publisher that created the event
                if (XanoSNCRepository.Instance.NotificationEventHasToken(notificationEvent, token))
                {
                    // Let the repository know that we are starting a notify, so that a Notification record
                    // can be created to track everything
                    notificationId = XanoSNCRepository.Instance.CreateNotification(publisher, notificationEvent);
                }
                else
                {
                    ThrowWebFault("Token " + token + " does not exist for the notificationEvent " + notificationEvent + " for publisher " + publisher, HttpStatusCode.BadRequest);
                }
            }
            catch (Exception e)
            {
                ThrowWebFaultOnRepositoryException(e);
            }

            // Find all the subscribers that want to know about this notification
            List<string> subscribers = null;
            try
            {
                subscribers = XanoSNCRepository.Instance.GetSubscribersForNotification(notificationEvent);
            }
            catch (Exception e)
            {
                ThrowWebFaultOnRepositoryException(e);
            }

            foreach (var subscriber in subscribers)
            {
                var errorMessage = string.Empty;    

                try
                {
                    // Try to notify each subscriber
                    NotifySubscriber(publisher, notificationEvent, subscriber, json);
                }
                catch(Exception e)
                {
                    // catch the exception. We are going to create a subscription notification record below
                    // and we need to log the fact that we ran into problems. 
                    errorMessage = e.Message;
                }

                try
                {
                    XanoSNCRepository.Instance.CreateSubscriptionNotification(notificationId, subscriber, errorMessage);
                }
                catch (Exception e)
                {
                    ThrowWebFaultOnRepositoryException(e);
                }
            }
        }

        async private void NotifySubscriber(string publisher, string notificationEvent, string subscriber, string json)
        {
            try
            {
                var notifyUrl = XanoSNCRepository.Instance.GetUrlFromSubscription(notificationEvent, subscriber);

                using (var httpClient = new HttpClient())
                {
                    var stringContent = new StringContent(json, Encoding.UTF8);

                    // POST the json object to the Url provided by the subscriber
                    var response = await httpClient.PostAsync(notifyUrl, null);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception("Status code is " + response.StatusCode);
                    }
                }
            }
            catch(Exception e)
            {
                ThrowWebFault(e.Message, HttpStatusCode.InternalServerError);
            }
        }

        private void ThrowWebFault(string message, HttpStatusCode statusCode)
        {
            var errorData = new ErrorData("Repository error", message);
            throw new WebFaultException<ErrorData>(errorData, statusCode);
        }

        private void ThrowWebFaultOnRepositoryException(Exception e)
        {
            var errorData = new ErrorData("Repository error", e.Message);
            throw new WebFaultException<ErrorData>(errorData, HttpStatusCode.InternalServerError);
        }

        public string TestGetMe(string test)
        {
            OperationContext context = OperationContext.Current;

            if (context != null)
            {
                MessageProperties messageProperties = context.IncomingMessageProperties;
            }

            return "GET Result is " + test;
        }

        public string TestPostMe(string test)
        {
            return "POST Result is " + test;
        }

        public void TestPostStream(string myString, Stream stream)
        {
            string jsonString = string.Empty;
            using (var reader = new StreamReader(stream))
            {
                jsonString = reader.ReadToEnd();
            }
        }

        public void TestNotifySubscriber(string subscriber, string notificationEvent, string subscriptionToken, string json)
        {
            // todo: Make sure this is the subscriber by validating the subscription token
            if (XanoSNCRepository.Instance.SubscriptionNotificationHasToken(subscriber, notificationEvent, subscriptionToken))
            {
                NotifySubscriber("None", notificationEvent, subscriber, json);
            }
            else
            {
                ThrowWebFault("No subscription found with subscription token " + subscriptionToken + ", for subscriber " + subscriber + ", for notificationEvent " + notificationEvent, HttpStatusCode.BadRequest);
            }            
        }
    }
}
