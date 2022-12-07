using Firebase.Auth;
using Firebase.Storage;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using UPOD.SERVICES.Services;

namespace UPOD.API.Controllers
{

    [ApiController]
    [Route("api/upload_image")]
    public class UploadImagesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public UploadImagesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpPost]
        public async Task<IActionResult> Index(IFormFile file)
        {
            var id = Guid.NewGuid();
            var fileUpload = file.OpenReadStream();
            //connect to firebase
            var auth = new FirebaseAuthProvider(new FirebaseConfig(_configuration["Firebase:ApiKey"]));
            var a = await auth.SignInWithEmailAndPasswordAsync(_configuration["Firebase:AuthEmail"], _configuration["Firebase:AuthPassword"]);

            var cancellation = new CancellationTokenSource();

            var task = new FirebaseStorage(_configuration["Firebase:Bucket"], new FirebaseStorageOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(a.FirebaseToken),
                ThrowOnCancel = true
            })
            .Child(id + "image.png")
            .PutAsync(fileUpload, cancellation.Token);
            return Ok(await task);
        }

    }
}
