//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DatabaseRepository
{
    using System;
    using System.Collections.Generic;
    
    public partial class Log
    {
        public long Id { get; set; }
        public string NodePublicKey { get; set; }
        public string Message { get; set; }
        public string Event { get; set; }
        public System.DateTime CreatedDate { get; set; }
    }
}