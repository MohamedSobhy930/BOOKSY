using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace BOOKSY.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage ="Name is Required")]
        [RegularExpression("^[a-zA-Z ]+$", ErrorMessage = "Name must contain only letters and spaces.")]
        [MaxLength(30)]
        [DisplayName("Category Name")]
        public string? Name { get; set; }
        [Range(1,100,ErrorMessage ="Display Order must be between 1 - 100")]
        [DisplayName("Display Order")]
        public int DisplayOrder { get; set; }
    }
}
