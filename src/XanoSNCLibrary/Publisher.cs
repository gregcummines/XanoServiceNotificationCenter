﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace XanoSNCLibrary
{
    [DataContract]
    public class Publisher
    {
        [DataMember]
        public string Name { get; set; }
    }
}
