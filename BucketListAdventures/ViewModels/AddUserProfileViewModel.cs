﻿using System.ComponentModel.DataAnnotations;

namespace BucketListAdventures.ViewModels
{
    public class AddUserProfileViewModel
    {
        [Required(ErrorMessage="Name is required")]
        public string Name { get; set; }
        [Required(ErrorMessage ="Address is required")]
        public string Address { get; set; }
        [Requires(ErrorMessage="Interests is required")]
        public string Interests { get; set; }
    }
}
