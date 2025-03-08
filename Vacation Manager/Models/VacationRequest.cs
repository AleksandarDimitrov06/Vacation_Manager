using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Vacation_Manager.Models
{

    public enum RequestType
    {
        Paid,
        Unpaid,
        Sick
    }
    public class VacationRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public DateTime CreationDate { get; set; }

        public bool HalfDay { get; set; }

        public bool Approved { get; set; }

        [Required]
        public string RequesterId { get; set; }

        [ForeignKey("RequesterId")]
        public virtual User Requester { get; set; }

        [Required]
        public RequestType RequestType { get; set; }

        [StringLength(255)]
        public string MedicalDocumentPath { get; set; }

        public string ApproverId { get; set; }

        [ForeignKey("ApproverId")]
        public virtual User Approver { get; set; }
    }
}
