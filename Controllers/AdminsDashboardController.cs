using BLL.Interfaces;
using BLL.Repositories;
using DAL.DTOs;
using DAL.DTOs.Auth;
using DAL.DTOs.SetUp;
using DAL.Entities.AppUser;
using DAL.Entities.Enums;
using DAL.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [Authorize(Policy = "ValidToken")]
    [ApiController]
    public class AdminsDashboardController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> user;
        private readonly IUnitOfWork unitOfWork;

        public AdminsDashboardController(UserManager<ApplicationUser> User, IUnitOfWork UnitOfWork)
        {
            user = User;
            unitOfWork = UnitOfWork;
        }
        [Authorize(Roles = "Admin,AdminAssistant")]
        [HttpGet("GetAllUsers")]
        public async Task<IActionResult> GetAllUsers()
        {
            var Users = await user.Users.ToListAsync();
            var UserWithRoles = new List<User>();
            foreach (var _user in Users)
            {
                var userRoles = await user.GetRolesAsync(_user);
                UserWithRoles.Add(new User
                {
                    UserId = _user.Id,
                    UserName = _user.UserName,
                    Email = _user.Email,
                    Roles = userRoles.ToList()
                });
            }
            return Ok(UserWithRoles);
        }
        [Authorize(Roles = "Admin")]
        [HttpPatch("ChangeUserRole")]
        public async Task<IActionResult> ChangeUserRole([FromForm] string username, RoleEnum newrole)
        {
            var User = await user.FindByNameAsync(username);
            if (User == null)
            {
                return NotFound();
            }
            var currentRoles = await user.GetRolesAsync(User);
            var removeResult = await user.RemoveFromRolesAsync(User, currentRoles);
            if (!removeResult.Succeeded)
            {
                return BadRequest(removeResult.Errors);
            }
            var AddRole = await user.AddToRoleAsync(User, newrole.ToString());
            return Ok("This User your role is updated");
        }
        [HttpGet("GetAllOrders")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetOrders()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleid = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userId != null)
            {
                if (roleid == "Admin" || roleid == "AdminAssistant")
                {

                    var AllOrders = unitOfWork.Generic<Orders>().GetAll().Where(O => O.IsDeleted == false).Include(oi => oi.OrderItems).Include(u => u.User).Select(o => new SetOrders
                    {
                        Id = o.Id,
                        UserId = o.UserId,
                        Username = o.User.UserName,
                        OrderId = o.Id,
                        OrderDate = o.OrderDate,
                        Status = o.Status,
                        OrderItems = o.OrderItems.Select(oi => new SetOrderItems
                        {
                            Id = oi.Id,
                            MenuItemId = oi.MenuItemId,
                            ItemName = oi.ItemName,
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            SubTotal = oi.SubTotal
                        }).ToList(),
                        TotalPrice = o.OrderItems.Sum(res => res.SubTotal)
                    });
                    return Ok(AllOrders);
                }
            }
            return Unauthorized();
        }
        [HttpGet("GetAllOrdersFeedback")]
        [Authorize(Roles= "Admin,AdminAssistant")]
        public async Task<IActionResult> GetAllOrdersFeedback()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var AllOrdersFeedBack = await unitOfWork.Generic<OrderFeedback>().GetAll().Where(ofb => ofb.IsDeleted == false).Include(o => o.Order).Include(u => u.User).Select(ofb => new SetOrderFeedBack
                    {
                        Id = ofb.Id,
                        UserId = ofb.UserId,
                        Username = ofb.User.UserName,
                        OrderId = ofb.OrderId,
                        Comment = ofb.Comment,
                        Rating = ofb.Rating,
                        SubmittedOn = ofb.SubmittedOn
                    }).ToListAsync();
                    return Ok(AllOrdersFeedBack);
                }
            }
            return Unauthorized();
        }
        [HttpGet("GetAllReservations")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetAllReservations()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleid = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userId != null)
            {
                if (roleid == "Admin" || roleid == "AdminAssistant")
                {
                    var AllReservations = unitOfWork.Generic<Reservation>().GetAll().Where(res => res.IsDeleted == false).Select(res => new SetReservation
                    {
                        ReservationId = res.Id,
                        Status = res.ReservationStatus,
                        UserId = res.UserId,
                        Username = res.User.UserName,
                        TableNumber = res.Table.TableNumber,
                        TableLocation = res.Table.Location,
                        NumberOfGuests = res.NumberOfGuests,
                        DateOfReservation = res.DateOfReservation,
                        StartDate = res.StartDate,
                        EndDate = res.EndDate
                    });
                    return Ok(AllReservations);
                }
            }
            return Unauthorized();
        }
        [HttpGet("GetAllReservationsFeedback")]
        [Authorize(Roles= "Admin,AdminAssistant")]
        public async Task<IActionResult> GetAllReservationsFeedback()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (userId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var AllReservationsFeedback = await unitOfWork.Generic<ReservationFeedback>().GetAll().Where(ofb => ofb.IsDeleted == false).Include(o => o.Reservation).Include(u => u.User).Select(ofb => new SetReservationFeedback
                    {
                        Id = ofb.Id,
                        UserId = ofb.UserId,
                        Username = ofb.User.UserName,
                        ReservationId = ofb.ReservationId,
                        Comment = ofb.Comment,
                        Rating = ofb.Rating,
                        SubmittedOn = ofb.SubmittedOn
                    }).ToListAsync();
                    return Ok(AllReservationsFeedback);
                }
            }
            return Unauthorized();
        }
        [HttpGet("GetAllComplaintandSuggestion")]
        [Authorize(Roles= "Admin,AdminAssistant")]
        public IActionResult GetAllComplaintandSuggestion()
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId == "Admin" || RoleId == "AdminAssistant")
            {
                var AllComplaintandSuggestion =  unitOfWork.Generic<ComplaintandSuggestion>().GetAll().Where(ofb => ofb.IsDeleted == false).Include(u => u.User).Select(ofb => new SetComplaintandSuggestion
                {
                    Id=ofb.Id,
                    UserId = ofb.User.Id,
                    Username = ofb.User.UserName,
                    Problemandsolving=ofb.Problemandsolving,
                    Date=ofb.Date,

                });
                return Ok(AllComplaintandSuggestion);
            }
            return Unauthorized();
        }
        [HttpGet("Systemdataanalysis")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> Systemdataanalysis()
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId == "Admin"||RoleId== "AdminAssistant")
            {
                var totalUsers = await user.Users.CountAsync();
                var ActiveUsers = await user.Users.CountAsync(u => u.IsDeleted == false);
                var totalOrders = await unitOfWork.Generic<Orders>().GetAll().CountAsync(to => to.IsDeleted == false);
                var OrderPaid=await unitOfWork.Generic<Orders>().GetAll().CountAsync(op=>op.IsDeleted == false && op.IsPaid==true);
                var OrderCancelled = await unitOfWork.Generic<Orders>().GetAll().CountAsync(oc=>oc.IsDeleted == true);
                var totalReservations = await unitOfWork.Generic<Reservation>().GetAll().CountAsync(tr => tr.IsDeleted == false);
                var ReservationPaid = await unitOfWork.Generic<Reservation>().GetAll().CountAsync(rp=>rp.IsDeleted == false&&rp.IsPaid==true);
                var ReservationCancelled = await unitOfWork.Generic<Reservation>().GetAll().CountAsync(rc=>rc.IsDeleted == true);
                var ReservationFeedbacks = await unitOfWork.Generic<ReservationFeedback>().GetAll().CountAsync(rfb => rfb.IsDeleted == false);                ;
                var OrderFeedbacks = await unitOfWork.Generic<OrderFeedback>().GetAll().CountAsync(ofb => ofb.IsDeleted == false);
                var dashboardData = new
                {
                    TotalUsers = totalUsers,
                    TotalOrders = totalOrders,
                    OrderPaid = OrderPaid,
                    OrderCancelled=OrderCancelled,
                    OrderFeedbacks=OrderFeedbacks,
                    TotalReservations = totalReservations,
                    ReservationPaid = ReservationPaid,
                    ReservationCancelled=ReservationCancelled,
                    ReservationFeedbacks=ReservationFeedbacks
                };
                return Ok(dashboardData);
            }
            return Unauthorized();
        }
    } 
}
