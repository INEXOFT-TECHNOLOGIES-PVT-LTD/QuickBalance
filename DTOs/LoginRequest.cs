using System.ComponentModel.DataAnnotations;

namespace QuickBalance.API.DTOs
{
    //public class LoginRequest
    //{
    //    public string LoginUserName { get; set; } = "";

    //    public string LoginPassword { get; set; } = "";
    //}
    public sealed class LoginRequest
    {
        /// <summary>
        /// Username used to authenticate the user.
        /// </summary>
        [Required(ErrorMessage = "Username is required.")]
        public string LoginUserName { get; set; } 

        /// <summary>
        /// Password used to authenticate the user.
        /// </summary>
        [Required(ErrorMessage = "Password is required.")]
        public string LoginPassword { get; set; }
    }
}