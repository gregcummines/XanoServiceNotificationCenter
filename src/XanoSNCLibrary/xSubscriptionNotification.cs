//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace XanoSNCLibrary
{
    using System;
    using System.Collections.Generic;
    
    public partial class xSubscriptionNotification
    {
        public int Id { get; set; }
        public int SubscriptionId { get; set; }
        public int NotificationId { get; set; }
        public string NotificationError { get; set; }
        public System.DateTime CreatedDate { get; set; }
    
        public virtual xNotification xNotification { get; set; }
        public virtual xSubscription xSubscription { get; set; }
    }
}
