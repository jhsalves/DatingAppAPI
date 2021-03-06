using System.Linq;
using AutoMapper;
using DatingApp.API.Dto;
using DatingApp.API.Models;

namespace DatingApp.API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            CreateMap<User,UserForListDto>()
                    .ForMember(dest => dest.PhotoUrl, opt => {
                    opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url);
                })
                .ForMember(dest => dest.Age, opt => {
                    opt.ResolveUsing(resolv => resolv.DateOfBirth.CalculateAge());
                });

            CreateMap<User,UserForDetailDto>()
                .ForMember(dest => dest.PhotoUrl, opt => {
                    opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url);
                })
                .ForMember(dest => dest.Age, opt => {
                    opt.ResolveUsing(resolv => resolv.DateOfBirth.CalculateAge());
                });

            CreateMap<Photo,PhotosForDetaileDto>();
            CreateMap<UserForUpdateDto,User>();
            CreateMap<PhotoForCreationDto,Photo>();
            CreateMap<Photo,PhotoForReturnDto>();
            CreateMap<UserForRegisterDto,User>();
                       CreateMap<MessageForCreationDto,Message>().ReverseMap()
                .ForMember(m => m.SenderPhotoUrl, opt => 
                     opt.MapFrom(u => u.Sender.Photos.FirstOrDefault(p => p.IsMain).Url));
            CreateMap<MessageForCreationDto,Message>().ReverseMap();
            CreateMap<Message, MessageForReturnDto>()
                .ForMember( m => m.SenderPhotoUrl, opt =>
                     opt.MapFrom(u => u.Sender.Photos.FirstOrDefault(p => p.IsMain).Url))
                     .ForMember( m => m.RecipientPhotoUrl, opt =>
                     opt.MapFrom(u => u.Recipient.Photos.FirstOrDefault(p => p.IsMain).Url));
        }
    }
}