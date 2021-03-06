﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers; // For AuthenticationHeaderValue
using System.Web;
using XanoSNCTestPublisher;

namespace XanoServiceNotificationCenterTestPublisher
{
    /// <summary>
    /// Resources:
    /// http://www.c-sharpcorner.com/UploadFile/dacca2/http-request-methods-get-post-put-and-delete/
    /// 
    /// </summary>
    class Program
    {
        static void Main()
        {
            //Publisher_CreateNotificationEvent();
            GetNotificationEvents();
            Publisher_SendNotification();
            Publisher_SendBadNotification();
        }

        static void Publisher_CreateNotificationEvent()
        {
            using (var httpClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true }))
            {
                var jsonSchema = "{\"$schema\":\"http://json-schema.org/draft-04/schema#\",\"id\":\"http://jsonschema.net\",\"type\":\"object\",\"properties\":{\"FirmwarePackageVersion\":{\"id\":\"http://jsonschema.net/FirmwarePackageVersion\",\"type\":\"string\"},\"FirmwareConfigurationVersion\":{\"id\":\"http://jsonschema.net/FirmwareConfigurationVersion\",\"type\":\"string\"}},\"required\":[\"FirmwarePackageVersion\",\"FirmwareConfigurationVersion\"]}";
                var stringContent = new StringContent(jsonSchema, Encoding.UTF8);
                var publisher = "Roka";
                var notificationEvent = "FirmwareRelease";

                var url = $"http://localhost:8733/XanoServiceNotificationCenter/createNotificationEvent/{publisher}/{notificationEvent}";
                url = HttpUtility.UrlPathEncode(url);
                var response = httpClient.PostAsync(url, stringContent).Result;
                if (response.IsSuccessStatusCode)
                {
                    Log("Success");
                }
                else
                {
                    var errorMessage = response.Content.ReadAsStringAsync().Result;
                    Log(errorMessage);
                }
            }
        }

        static void GetNotificationEvents()
        {
            using (var httpClient = new HttpClient(new LoggingHandler(new HttpClientHandler() { UseDefaultCredentials = true })))
            {
                var url = "http://localhost:8733/XanoServiceNotificationCenter/getNotificationEvents";
                var httpResponseMessage = httpClient.GetAsync(url);
                var result = httpResponseMessage.Result;
                if (result.IsSuccessStatusCode)
                {
                    var jsonResult = result.Content.ReadAsStringAsync().Result;
                    var notificationEvents = JsonConvert.DeserializeObject<List<string>>(jsonResult);
                }
                else
                {
                    // handle the bad status code
                    var jsonResult = result.Content.ReadAsStringAsync().Result;
                    Log("Error. The status code was " + result.StatusCode + ". " + jsonResult);
                }
            }
        }

        static async void Publisher_SendNotification()
        {
            using (var httpClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true }))
            {
                string json = "{ \"FirmwarePackageVersion\": \"5.0.1.9\", \"FirmwareConfigurationVersion\": \"9.1.1.3.0\" }";
                var stringContent = new StringContent(json, Encoding.UTF8);

                // UriTemplate: notifySubscribers/{publisher}/{notificationEvent}/{token}
                var url = "http://localhost:8733/XanoServiceNotificationCenter/notifySubscribers/Roka/FirmwareRelease/d54ab82f-21f2-42f3-9e43-63034f0ad52c";
                var response = httpClient.PostAsync(url, stringContent).Result;
                var jsonResult = response.Content.ReadAsStringAsync().Result;
                
                if (response.IsSuccessStatusCode)
                {
                    Log("Success");
                }
                else
                {
                    Log("Error. The status code was " + response.StatusCode + ". " + jsonResult);
                }
            }
        }

        static async void Publisher_SendBadNotification()
        {
            using (var httpClient = new HttpClient(new HttpClientHandler() { UseDefaultCredentials = true }))
            {
                string json = "{ \"FirmwarePackageVersion\": \"5.0.1.9\", \"FirmwareConfigurationVersion\": \"9.1.1.3.0\" }";
                var stringContent = new StringContent(json, Encoding.UTF8);

                // UriTemplate: notifySubscribers/{publisher}/{notificationEvent}/{token}
                var url = "http://localhost:8733/XanoServiceNotificationCenter/notifySubscribers/Roka/FirmwareRelease/e54ab82f-21f2-42f3-9e43-63034f0ad52c";
                var response = httpClient.PostAsync(url, stringContent).Result;
                var jsonResult = response.Content.ReadAsStringAsync().Result;
                
                if (response.IsSuccessStatusCode)
                {
                    Log("Success");
                }
                else
                {
                    Log("Error. The status code was " + response.StatusCode + ". " + jsonResult);
                }
            }
        }

        static void Log(string message)
        {
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }
    }
}
