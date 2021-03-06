﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace XanoSNCLibrary
{
    /// <summary>
    /// Interface used to manage the Xano SNC persistent data
    /// </summary>
    public class XanoSNCRepository : IXanoSNCRepository
    {
        /// <summary>
        /// Stores a new notification type in the database
        /// </summary>
        /// <param name="publisher"></param>
        /// <param name="notificationEvent"></param>
        /// <param name="jsonSchema">json Schema for the notification message that will be sent to subscribers</param>
        /// <returns>A token that the publisher must use in subsequent calls to NotifySubscribers</returns>
        public string CreateNotificationEvent(string publisher, string notificationEvent, string jsonSchema)
        {
            using (var db = new XanoSNCEntities())
            {
                var publisherId = 0;
                var xPublisher = (from p in db.xPublishers
                                  where p.Name == publisher
                                  select p).SingleOrDefault();
                if (xPublisher == null)
                {
                    var newPublisher = new xPublisher()
                    {
                        Name = publisher,
                        CreatedDate = DateTime.Now
                    };
                    db.xPublishers.Add(newPublisher);
                    publisherId = newPublisher.Id;
                }
                else
                {
                    publisherId = xPublisher.Id;
                }

                var token = Guid.NewGuid().ToString();

                var xNotificationEvent = (from ne in db.xNotificationEvents
                                  where ne.Name == notificationEvent
                                  select ne).SingleOrDefault();
                if (xNotificationEvent == null)
                {
                    db.xNotificationEvents.Add(new xNotificationEvent()
                    {
                        Name = notificationEvent,
                        PublisherId = publisherId,
                        JsonSchema = jsonSchema,                
                        Token = token,
                        CreatedDate = DateTime.Now
                    });
                }
                else
                {
                    throw new Exception("Notification: " + notificationEvent + " already exists.");
                }

                db.SaveChanges();

                return token;
            }
        }

        /// <summary>
        /// Updates the Json schema associated with an existing notification event defined by the token
        /// </summary>
        /// <param name="token"></param>
        /// <param name="jsonSchemaString"></param>
        public void UpdateNotificationEventJsonSchema(
            string token,
            string jsonSchemaString)
        {
            using (var db = new XanoSNCEntities())
            {
                var notificationEvent = (from ne in db.xNotificationEvents
                                        where ne.Token == token
                                        select ne).SingleOrDefault();
                if (notificationEvent != null)
                {
                    throw new Exception("No notification event found with token: " + token);
                }
                else
                {
                    notificationEvent.JsonSchema = jsonSchemaString;
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Gets a list of all notification types from the database
        /// </summary>
        /// <returns></returns>
        public List<string> GetNotificationEvents()
        {
            using (var db = new XanoSNCEntities())
            {
                return (from ne in db.xNotificationEvents
                        join p in db.xPublishers on ne.PublisherId equals p.Id
                          select ne.Name).ToList();
            }
        }

        /// <summary>
        /// Gets a list of all active subscribers
        /// </summary>
        /// <returns></returns>
        public List<Subscriber> GetSubscribers()
        {
            using (var db = new XanoSNCEntities())
            {
                return (from s in db.xSubscribers
                        select new Subscriber()
                        {
                            Name = s.Name
                        }).ToList();
            }
        }

        public string GetNotificationEventMessageSchema(string notificationEvent)
        {
            using (var db = new XanoSNCEntities())
            {
                var schema = (from ne in db.xNotificationEvents
                              where ne.Name == notificationEvent
                              select ne.JsonSchema).Single();

                return schema;
            }
        }

        /// <summary>
        /// Gets an active list of notifications for the subscriber
        /// </summary>
        /// <param name="subscriber"></param>
        /// <returns></returns>
        public List<NotificationEvent> GetNotificationsForSubscriber(Subscriber subscriber)
        {
            using (var db = new XanoSNCEntities())
            {
                return (from sb in db.xSubscriptions
                        join ne in db.xNotificationEvents on sb.NotificationEventId equals ne.Id
                        join sc in db.xSubscribers on sb.SubscriberId equals sc.Id
                        where sc.Name == subscriber.Name
                        select new NotificationEvent()
                        {
                            Name = ne.Name
                        }).ToList();
            }
        }

        public string GetUrlFromSubscription(string notificationEvent, string subscriber)
        {
            using (var db = new XanoSNCEntities())
            {
                var xNotificationEvent = (from ne in db.xNotificationEvents
                                          where ne.Name == notificationEvent
                                          select ne).SingleOrDefault();
                if (xNotificationEvent == null)
                {
                    throw new Exception("Notification event with name: " + notificationEvent + " has not been published.");
                }

                var subscriberId = 0;
                var xSubscriber = (from s in db.xSubscribers
                                   where s.Name == subscriber
                                   select s).SingleOrDefault();
                if (xSubscriber == null)
                {
                    throw new Exception("Subscriber " + subscriber + " does not exist");
                }
                else
                {
                    subscriberId = xSubscriber.Id;
                }

                var notifyUrl = (from sc in db.xSubscriptions
                                 join sb in db.xSubscribers on sc.SubscriberId equals sb.Id
                                 join ne in db.xNotificationEvents on sc.NotificationEventId equals ne.Id
                                 select sc.NotifyURL).FirstOrDefault();

                if (notifyUrl == null)
                {
                    throw new Exception("Subscription for " + notificationEvent + "not found for " + subscriber);
                }
                return notifyUrl;
            }
        }

        public bool NotificationEventHasToken(string notificationEvent, string token)
        {
            using (var db = new XanoSNCEntities())
            {
                var exists = ((from ne in db.xNotificationEvents
                               where ne.Token == token
                               select ne.Id).Count() > 0);
                return exists;
            }
        }

        /// <summary>
        /// Gets a list of subscribers for active notifications
        /// </summary>
        /// <param name="notification"></param>
        /// <returns></returns>
        public List<string> GetSubscribersForNotification(string notification)
        {
            using (var db = new XanoSNCEntities())
            {
                var subscribers = (from sc in db.xSubscriptions
                                   join sb in db.xSubscribers on sc.SubscriberId equals sb.Id
                                   join ne in db.xNotificationEvents on sc.NotificationEventId equals ne.Id
                                   where ne.Name == notification
                                   select sb.Name).ToList();
                return subscribers;
            }
        }

        public bool SubscriptionNotificationHasToken(string subscriber, string notificationEvent, string token)
        {
            using (var db = new XanoSNCEntities())
            {
                var exists = ((from sb in db.xSubscriptions
                             join sc in db.xSubscribers on sb.SubscriberId equals sc.Id
                             join ne in db.xNotificationEvents on sb.NotificationEventId equals ne.Id
                             where sb.Token == token && sc.Name == subscriber && ne.Name == notificationEvent
                             select sb.Id).Count() > 0);

                return exists;
            }
        }

        /// <summary>
        /// Stores a service subscription to a notification in the database
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="notificationEvent"></param>
        /// <param name="notifyUrl"></param>
        /// <returns>A token that must be used when unsubscribing</returns>
        public string Subscribe(string subscriber, string notificationEvent, string emailAddress, string notifyUrl)
        {
            using (var db = new XanoSNCEntities())
            {
                var token = Guid.NewGuid().ToString();
                var xNotificationEvent = (from ne in db.xNotificationEvents
                                          where ne.Name == notificationEvent
                                          select ne).SingleOrDefault();
                if (xNotificationEvent == null)
                {
                    throw new Exception("Notification event with name: " + notificationEvent + " has not been published.");
                }

                var subscriberId = 0;
                var xSubscriber = (from s in db.xSubscribers
                                  where s.Name == subscriber
                                  select s).SingleOrDefault();
                if (xSubscriber == null)
                {
                    var newSubscriber = new xSubscriber()
                    {
                        Name = subscriber,
                        CreatedDate = DateTime.Now
                    };
                    db.xSubscribers.Add(newSubscriber);
                    subscriberId = newSubscriber.Id;
                }
                else
                {
                    subscriberId = xSubscriber.Id;
                }

                var xSubscriptionDB = (from sc in db.xSubscriptions
                                     join sb in db.xSubscribers on sc.SubscriberId equals sb.Id
                                     join ne in db.xNotificationEvents on sc.NotificationEventId equals ne.Id
                                     where ne.Name == notificationEvent && sb.Name == subscriber 
                                     select new
                                     {
                                         SubscriberName = sb.Name, 
                                         NotificationEvent = ne.Name,
                                         NotificationEventId = ne.Id,
                                         SubscriberId = sb.Id
                                     }).SingleOrDefault();
                if (xSubscriptionDB == null)
                {
                    db.xSubscriptions.Add(new xSubscription()
                    {
                        NotificationEventId = xNotificationEvent.Id,
                        SubscriberId = subscriberId,
                        NotifyURL = notifyUrl,
                        Token = token,
                        EmailAddress = emailAddress,
                        CreatedDate = DateTime.Now
                    });
                }
                else
                {
                    throw new Exception("Subscription: " + notificationEvent + " already exists.");
                }

                db.SaveChanges();

                return token;
            }
        }

        public string GetEMailFromSubscription(string notificationEvent, string subscriber)
        {
            using (var db = new XanoSNCEntities())
            {
                var email = (from sc in db.xSubscriptions
                             join sb in db.xSubscribers on sc.SubscriberId equals sb.Id
                             join ne in db.xNotificationEvents on sc.NotificationEventId equals ne.Id
                             where sb.Name == subscriber && ne.Name == notificationEvent
                             select sc.EmailAddress).SingleOrDefault();
                return email;
            }
        }

        public void UpdateSubscriptionNotificationWithError(int subscriptionNotificationId, string errorMessage)
        {
            using (var db = new XanoSNCEntities())
            {
                var xSubscriptionNotification = (from s in db.xSubscriptionNotifications
                                                 where s.Id == subscriptionNotificationId
                                                 select s).Single();
                xSubscriptionNotification.NotificationError = errorMessage;
                db.SaveChanges();
            }
        }

        public int CreateSubscriptionNotification(int notificationId, string subscriber)
        {
            using (var db = new XanoSNCEntities())
            {
                var xSubscriber = (from s in db.xSubscribers
                                   where s.Name == subscriber
                                   select s).SingleOrDefault();
                if (xSubscriber == null)
                {
                    throw new Exception("Subscriber with name: " + subscriber + " does not exist.");
                }

                var subscriptionNotification = new xSubscriptionNotification()
                {
                    NotificationId = notificationId,
                    SubscriptionId = xSubscriber.Id,
                    CreatedDate = DateTime.Now
                };

                db.xSubscriptionNotifications.Add(subscriptionNotification);

                db.SaveChanges();

                return subscriptionNotification.Id;
            }            
        }

        /// <summary>
        /// Removes a service subscription to a notification in the database
        /// </summary>
        /// <param name="subscriber"></param>
        /// <param name="notificationEvent"></param>
        /// <param name="token">A token issued by the subscribe method</param>
        public void Unsubscribe(string subscriber, string notificationEvent, string token)
        {
            using (var db = new XanoSNCEntities())
            {
                var xNotificationEvent = (from ne in db.xNotificationEvents
                                          where ne.Name == notificationEvent
                                          select ne).SingleOrDefault();
                if (xNotificationEvent == null)
                {
                    throw new Exception("Notification event with name: " + notificationEvent + " has not been published.");
                }

                var xSubscriber = (from s in db.xSubscribers
                                   where s.Name == subscriber
                                   select s).SingleOrDefault();
                if (xSubscriber == null)
                {
                    throw new Exception("No subscriber with name: " + subscriber + " found.");
                }

                var xSubscriptionDB = (from sc in db.xSubscriptions
                                       join sb in db.xSubscribers on sc.SubscriberId equals sb.Id
                                       join ne in db.xNotificationEvents on sc.NotificationEventId equals ne.Id
                                       where ne.Name == notificationEvent && sb.Name == subscriber && sc.Token == token
                                       select new
                                       {
                                           SubscriberName = sb.Name,
                                           NotificationEvent = ne.Name,
                                           NotificationEventId = ne.Id,
                                           SubscriberId = sb.Id,
                                           SubscriptionId = sc.Id
                                       }).SingleOrDefault();
                if (xSubscriptionDB == null)
                {
                    throw new Exception("Subscription for subscriber " + subscriber + " for notification event " + notificationEvent + " not found with token " + token);
                }
                else
                {
                    var subscriptionDB = (from sc in db.xSubscriptions
                                          where sc.Id == xSubscriptionDB.SubscriptionId
                                          select sc).SingleOrDefault();
                    // Delete the subscription
                    db.xSubscriptions.Remove(subscriptionDB);
                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// Called by the WCF service when an attempt is made to notify everyone of a notification event
        /// Tracks an outgoing SNC notification attempt to subscribers in the database
        /// </summary>
        /// <param name="notificationEvent"></param>
        public int CreateNotification(string publisher, string notificationEvent, string message)
        {
            // Insert a notification record
            using (var db = new XanoSNCEntities())
            {
                // Lookup the notification event by name 
                var notificationEventDB = (from ne in db.xNotificationEvents
                                           where ne.Name == notificationEvent
                                           select ne).SingleOrDefault();
                if (notificationEventDB == null)
                    throw new Exception("Notification by name: " + notificationEvent + " does not exist");

                var publisherDB = (from p in db.xPublishers
                                           where p.Name == publisher
                                           select p).SingleOrDefault();
                if (publisherDB == null)
                    throw new Exception("Publisher by name: " + publisher + " does not exist");

                var notification = new xNotification()
                {
                    NotificationEventId = notificationEventDB.Id,
                    PublisherId = publisherDB.Id,
                    Message = message,
                    CreatedDate = DateTime.Now,
                };

                db.xNotifications.Add(notification);

                db.SaveChanges();

                return notification.Id;
            }
        }

        /// <summary>
        /// Called by the WCF service for each subscriber that we attempted to called
        /// </summary>
        /// <param name="notificationId">Id of the notification record</param>
        /// <param name="subscriber">Subscriber we attempted to contact</param>
        /// <param name="errorMessage">If an error occurred, this will be a non-null string with the message</param>
        public void CreateSubscriptionNotification(int notificationId, string subscriber, string errorMessage)
        {
            // Find the subscription id by subscriber name and notification event name
            // We need it to create a list of records for each subscriber that we attemped to contact
            using (var db = new XanoSNCEntities())
            {
                var subscriptionDB = (from sb in db.xSubscriptions
                                      join sc in db.xSubscribers on sb.SubscriberId equals sc.Id
                                      select sb).SingleOrDefault();

                if (subscriptionDB == null)
                    throw new Exception("Subscription does not exist!");

                var subscriptionNotification = new xSubscriptionNotification()
                {
                    CreatedDate = DateTime.Now,
                    NotificationError = errorMessage,
                    SubscriptionId = subscriptionDB.Id,
                };

                db.xSubscriptionNotifications.Add(subscriptionNotification);
                db.SaveChanges();
            }
        }
    }
}
