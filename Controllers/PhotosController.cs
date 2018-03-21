using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.Dto;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    public class PhotosController : Controller
    {
        public IDatingRepository _repo { get; }
        public IMapper _mapper { get; }
        public readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        public Cloudinary _cloudinary;

        public PhotosController(IDatingRepository repo,
         IMapper mapper,
         IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _cloudinaryConfig = cloudinaryConfig;
            _repo = repo;
            _mapper = mapper;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id){
            var photoFromRepo = await _repo.GetPhoto(id);

            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);

            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> addPhotoForUser(int userId, PhotoForCreationDto photoForCreationDTO){
            var user = await _repo.GetUser(userId);

            if(user == null){
                return BadRequest();
            }

            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);   

            if(currentUserId != user.Id){
                return Unauthorized();
            }

            var file = photoForCreationDTO.File;

            var uploadResult = new ImageUploadResult();

            if(file.Length > 0){
                using(var stream = file.OpenReadStream()){
                    var uploadParams = new ImageUploadParams{
                        File = new FileDescription(file.Name,stream),
                        Transformation = new Transformation().Width(500).Height(500).Crop("fill").Gravity("face")
                    };

                    uploadResult = _cloudinary.Upload(uploadParams);

                }
            }

            photoForCreationDTO.Url = uploadResult.Uri.ToString();
            photoForCreationDTO.PublicId = uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDTO);

            photo.User = user;

            if(!user.Photos.Any(m => m.IsMain)){
                photo.IsMain = true;
            }
            
            user.Photos.Add(photo);

            
            if(await _repo.SaveAll()){
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto",new { id = photo.Id}, photoToReturn);
            }

            return BadRequest("Could not add the photo");
        }

        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id){
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
                return Unauthorized();
            
            var photoFromRepo = await _repo.GetPhoto(id);

            if(photoFromRepo == null)
            return NotFound();

            if(photoFromRepo.IsMain){
                return BadRequest("This is already the main photo");
            }

            var currentMainPhoto = await _repo.GetMainPHotoForUser(userId);

            if(currentMainPhoto != null) {
                currentMainPhoto.IsMain = false;
            }

            photoFromRepo.IsMain = true;

            if(await _repo.SaveAll()){
                return NoContent();
            }

            return BadRequest();

        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id){

            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }

            var photoFromRepo = await _repo.GetPhoto(id);
            if(photoFromRepo == null){
                return NotFound();
            }

            if(photoFromRepo.IsMain){
                return BadRequest("You cannot delete main photo.");
            }

            if(photoFromRepo.PublicId != null){
                var deleteParams = new DeletionParams(photoFromRepo.PublicId);
                var result = _cloudinary.Destroy(deleteParams);
            }

            _repo.Delete(photoFromRepo);

            if(await _repo.SaveAll()){
                return Ok();
            }

            return BadRequest("Failed to delete the photo");
        }
    }
}