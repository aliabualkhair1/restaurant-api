using AutoMapper;
using DAL.DTOs.Auth;
using DAL.DTOs.Models.Add;
using DAL.DTOs.Models.Update;
using DAL.Entities.AppUser;
using DAL.Entities.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.DTOs.MappingProfile
{
    public class MapProfile : Profile
    {
        public MapProfile()
        {
            CreateMap<Menu, AddMenuDTO>().ReverseMap();
            CreateMap<Reservation, AddReservationDTO>().ReverseMap();
            CreateMap<ReservationFeedback, AddReservationFeedbackDTO>().ReverseMap();
            CreateMap<OrderFeedback, AddOrderFeedbackDTO>().ReverseMap();
            CreateMap<Category, AddCategoryDTO>().ReverseMap();
            CreateMap<ComplaintandSuggestion, AddComplaintandSuggestionDTO>().ReverseMap();
            CreateMap<MenuItems, AddMenuItemsDTO>().ReverseMap();
            CreateMap<OrderItems, AddOrderItemsDTO>().ReverseMap();
            CreateMap<Orders,AddOrderDTO>().ReverseMap();
            CreateMap<Tables, AddTablesDTO>().ReverseMap();
        }
    }
}