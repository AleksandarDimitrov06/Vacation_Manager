﻿using System.ComponentModel.DataAnnotations;

namespace Vacation_Manager.Models
{
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }

        [Required]
        [StringLength(100)]
        public required string ProjectName { get; set; }

        [Required]
        [StringLength(500)]
        public required string ProjectDescription { get; set; }
        public virtual ICollection<Team>? Teams { get; set; } = new List<Team>();

    }
}
