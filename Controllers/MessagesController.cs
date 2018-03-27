using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dto;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
    [ServiceFilter(typeof(LogUserActivity))]
    [Authorize]
    [Route("api/users/{userId}/[controller]")]
    public class MessagesController : Controller
    {
        private readonly IMapper _mapper;
        private IDatingRepository _repo;

        public MessagesController(IDatingRepository repository, IMapper mapper)
        {
            _mapper = mapper;
            _repo = repository;
        }

        [HttpGet("{id}", Name="GetMessage")]
        public async Task<IActionResult> GetMessage(int userId,int id){
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }
            var messageFromRepo = await _repo.GetMessage(id);

            if(messageFromRepo == null){
                return NotFound();
            }

            return Ok(messageFromRepo);
        }

        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkMessageAsRead(int userId, int id){
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }
            var messageFromRepo = await _repo.GetMessage(id);

            if(messageFromRepo.RecipientId != userId) {
                return BadRequest("Failed to mark message as read.");
            }

            if(messageFromRepo.RecipientId != userId){
                return BadRequest("Failed to mark message as read.");
            }

            messageFromRepo.IsRead = true;

            messageFromRepo.DateRead = DateTime.Now;

            await _repo.SaveAll();

            return NoContent();
        }


        [HttpGet("thread/{id}")]
        public async Task<IActionResult> GetMessageThread(int userId, int id){
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }

            var messagesFromRepo = await _repo.GetMessageThread(userId,id);

            var messageThread = _mapper.Map<IEnumerable<MessageForReturnDto>>(messagesFromRepo);

            return Ok(messageThread);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessagesForUser(int userId, MessageParams messageParams){
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }

            var messageFromRepo = await _repo.GetMessagesForUser(messageParams);

            var messages = _mapper.Map<IEnumerable<MessageForCreationDto>>(messageFromRepo);
        
            Response.AddPagination(messageFromRepo.CurrentPage, messageFromRepo.PageSize,
                     messageFromRepo.TotalCount, messageFromRepo.TotalPages);

            return Ok(messages);
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage(int userId, [FromBody] MessageForCreationDto messageForCreation){
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }

            messageForCreation.SenderId = userId;

            var recipient = await _repo.GetUser(messageForCreation.RecipientId);

            if(recipient == null){
                return BadRequest("Could not find user.");
            }

            var message = _mapper.Map<Message>(messageForCreation);

            _repo.Add(message);

            var messageToReturn = _mapper.Map<MessageForReturnDto>(message);

            if(await _repo.SaveAll()){
                messageToReturn.SenderPhotoUrl = messageForCreation.SenderPhotoUrl;
                messageToReturn.SenderKnownAs = messageForCreation.SenderKnownAs;
                return CreatedAtRoute("GetMessage",new {id = message.Id}, messageToReturn);
            }

            throw new Exception("Creating the message failed to save.");
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> DeleteMessage(int id, int userId){
            
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value)){
                return Unauthorized();
            }

            var messageFromRepo = await _repo.GetMessage(id);

            if(messageFromRepo.SenderId == userId){
                messageFromRepo.SenderDeleted = true;
            }

            if(messageFromRepo.RecipientId == userId){
                messageFromRepo.RecipientDeleted = true;
            }

            if(messageFromRepo.SenderDeleted || messageFromRepo.RecipientDeleted){
                _repo.Delete(messageFromRepo);
            }

            if(await _repo.SaveAll()){
                return NoContent();
            }

            throw new Exception("Error deleting the message.");
        }

    }
}