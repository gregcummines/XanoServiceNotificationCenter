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
    
    public partial class xSubscription
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public xSubscription()
        {
            this.xSubscriptionNotifications = new HashSet<xSubscriptionNotification>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public int SubscriberId { get; set; }
        public int NotificationEventId { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public string NotifyURL { get; set; }
        public string Token { get; set; }
    
        public virtual xNotificationEvent xNotificationEvent { get; set; }
        public virtual xSubscriber xSubscriber { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<xSubscriptionNotification> xSubscriptionNotifications { get; set; }
    }
}
