using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BMSBT.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public int LogId { get; set; }
        
        [Required]
        public string TableName { get; set; }
        
        [Required]
        public string Operation { get; set; }
        
        public string? RecordId { get; set; }
        
        public string? OldData { get; set; }
        
        public string? NewData { get; set; }
        
        public string? ChangedBy { get; set; }
        
        public DateTime ChangedAt { get; set; }
        
        public string? ModuleName { get; set; }
        
        public string? IPAddress { get; set; }
    }
}
