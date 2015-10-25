﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace XanoHubLibrary
{
    [DataContract]
    public class Notification
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string ServiceOwner { get; set; }
    }
}
