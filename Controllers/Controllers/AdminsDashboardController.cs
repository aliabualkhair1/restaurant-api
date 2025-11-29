using BLL.Interfaces;
using BLL.Repositories;
using DAL.DTOs;
using DAL.DTOs.SetUp;
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
                    IsDeleted=_user.IsDeleted,
                    Roles = userRoles.ToList()
                });
            }
            return Ok(UserWithRoles);
        }
        [Authorize(Roles = "Admin,AdminAssistant")]
        [HttpGet("GetUser")]
        public async Task<IActionResult> GetUser(string name)
        {
            var User = await user.Users.FirstOrDefaultAsync(n=>n.UserName==name);
            if (User != null)
            {
                var UserWithRoles = new List<User>();
                var userRoles = await user.GetRolesAsync(User);
                var _user=new User
                {
                    UserId = User.Id,
                    UserName = User.UserName,
                    Email = User.Email,
                    IsDeleted = User.IsDeleted,
                    Roles = userRoles.ToList()
                };
                return Ok(_user);
            }
            else
            {
                return NotFound("لم يتم العثور على هذا المستخدم");
            }
        }
        [Authorize(Roles = "Admin")]
        [HttpPatch("ChangeUserRole")]
        public async Task<IActionResult> ChangeUserRole([FromBody] UpdateRole updaterole)
        {
            var User = await user.FindByNameAsync(updaterole.username);
            if (User == null)
            {
                return BadRequest("بيانات غير صالحة");
            }
            var currentRoles = await user.GetRolesAsync(User);
            var removeResult = await user.RemoveFromRolesAsync(User, currentRoles);
            if (!removeResult.Succeeded)
            {
                return BadRequest(removeResult.Errors);
            }
            var AddRole = await user.AddToRoleAsync(User, updaterole.newrole.ToString());
            return Ok("تم تحديث دور المستخدم بنجاح");
        }
        [HttpGet("GetAllCustomersQuestions")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetAllCustomersQuestions()
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var AllCustomersQuestions = await unitOfWork.Generic<ContactUs>().GetAll().Where(ofb => ofb.IsDeleted == false).Select(ofb => new SetContactUs
                    {
                        Id = ofb.Id,
                        FullName = ofb.FullName,
                        Email = ofb.Email,
                        Message = ofb.Message,
                        DateOfSending = ofb.DateOfSending,
                        
                        
                    }).ToListAsync();
                    return Ok(AllCustomersQuestions);
                }
            }
            return Unauthorized();
        }
        [HttpGet("GetAllOrders")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetOrders()
        {
            var roleid = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleid != null)
            {
                if (roleid == "Admin" || roleid == "AdminAssistant")
                {

                    var AllOrders = unitOfWork.Generic<Orders>().GetAll().Where(O => O.IsDeleted == false&&O.Status==OrderStatus.InProgress).Include(oi => oi.OrderItems).Include(u => u.User).Select(o => new SetOrders
                    {
                        UserId = o.UserId,
                        Username = o.User.UserName,
                        OrderId = o.Id,
                        OrderDate = o.OrderDate,
                        Status = o.Status,
                        OrderItems = o.OrderItems.Select(oi => new SetOrderItems
                        {
                            Id = oi.Id,
                            MenuItemId = oi.MenuItemId,
                            MenuName = oi.MenuItem.Menu.MenuName,
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
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetAllOrdersFeedback()
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
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
        [HttpGet("GetUserOrdersFeedback")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetUserOrdersFeedback(string userid)
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var AllOrdersFeedBack = await unitOfWork.Generic<OrderFeedback>().GetAll().Where(ofb => ofb.IsDeleted == false && ofb.UserId == userid).Include(o => o.Order).Include(u => u.User).Select(ofb => new SetOrderFeedBack
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
            var roleid = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleid != null)
            {
                if (roleid == "Admin" || roleid == "AdminAssistant")
                {
                    var AllReservations = unitOfWork.Generic<Reservation>().GetAll().Where(res => res.IsDeleted == false&&res.ReservationStatus==ReservationStatus.InProgress).Select(res => new SetReservation
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
                        EndDate = res.EndDate,
                        IsPaid = res.IsPaid
                    });
                    return Ok(AllReservations);
                }
            }
            return Unauthorized();
        }
        [HttpGet("GetUserReservations")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetUserReservations(string userid)
        {
            var roleid = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleid != null)
            {
                if (roleid == "Admin" || roleid == "AdminAssistant")
                {
                    var AllReservations = unitOfWork.Generic<Reservation>().GetAll().Where(res => res.IsDeleted == false && res.ReservationStatus == ReservationStatus.InProgress && res.UserId == userid).Select(res => new SetReservation
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
                        EndDate = res.EndDate,
                        IsPaid = res.IsPaid
                    });
                    return Ok(AllReservations);
                }
            }
            return Unauthorized();
        }
        [HttpGet("GetAllReservationsFeedback")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetAllReservationsFeedback()
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
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
        [HttpGet("GetUserReservationsFeedback")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> GetUserReservationsFeedback(string userid)
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var AllReservationsFeedback = await unitOfWork.Generic<ReservationFeedback>().GetAll().Where(ofb => ofb.IsDeleted == false && ofb.UserId == userid).Include(o => o.Reservation).Include(u => u.User).Select(ofb => new SetReservationFeedback
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
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetAllComplaintandSuggestion()
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId == "Admin" || RoleId == "AdminAssistant")
            {
                var AllComplaintandSuggestion = unitOfWork.Generic<ComplaintandSuggestion>().GetAll().Where(ofb => ofb.IsDeleted == false).Include(u => u.User).Select(ofb => new SetComplaintandSuggestion
                {
                    Id = ofb.Id,
                    UserId = ofb.User.Id,
                    Username = ofb.User.UserName,
                    Problemandsolving = ofb.Problemandsolving,
                    Date = ofb.Date,

                });
                return Ok(AllComplaintandSuggestion);
            }
            return Unauthorized();
        }
        [HttpGet("GetUserComplaintandSuggestion")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetUserComplaintandSuggestion(string userid)
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var AllComplaintandSuggestion = unitOfWork.Generic<ComplaintandSuggestion>().GetAll().Where(ofb => ofb.IsDeleted == false && ofb.UserId == userid).Include(u => u.User).Select(ofb => new SetComplaintandSuggestion
                    {
                        Id = ofb.Id,
                        UserId = ofb.User.Id,
                        Username = ofb.User.UserName,
                        Problemandsolving = ofb.Problemandsolving,
                        Date = ofb.Date,
                    });
                    return Ok(AllComplaintandSuggestion);
                }
                return Unauthorized();
            }
            return Unauthorized();
        }

        [HttpGet("orderspaid")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult orderspaid()
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var orderspaid = unitOfWork.Generic<Orders>().GetAll().Where(op => op.IsPaid == true).Include(u => u.User).Select(o => new SetOrders
                    {
                        UserId = o.UserId,
                        Username = o.User.UserName,
                        OrderId = o.Id,
                        OrderDate = o.OrderDate,
                        Status = o.Status,
                        OrderItems = o.OrderItems.Select(oi => new SetOrderItems
                        {
                            Id = oi.Id,
                            MenuItemId = oi.MenuItemId,
                            MenuName = oi.MenuItem.Menu.MenuName,
                            ItemName = oi.ItemName,
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            SubTotal = oi.SubTotal
                        }).ToList(),
                        TotalPrice = o.OrderItems.Sum(res => res.SubTotal),
                        IsPaid = o.IsPaid
                    });
                    return Ok(orderspaid);
                }
                return Unauthorized();
            }
            return Unauthorized();
        }

        [HttpGet("userorderspaid")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult userorderspaid(string userid)
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var orderspaid = unitOfWork.Generic<Orders>().GetAll().Where(op => op.IsPaid == true && op.UserId == userid).Include(u => u.User).Select(o => new SetOrders
                    {
                        UserId = o.UserId,
                        Username = o.User.UserName,
                        OrderId = o.Id,
                        OrderDate = o.OrderDate,
                        Status = o.Status,
                        OrderItems = o.OrderItems.Select(oi => new SetOrderItems
                        {
                            Id = oi.Id,
                            MenuItemId = oi.MenuItemId,
                            MenuName = oi.MenuItem.Menu.MenuName,
                            ItemName = oi.ItemName,
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            SubTotal = oi.SubTotal
                        }).ToList(),
                        TotalPrice = o.OrderItems.Sum(res => res.SubTotal),
                        IsPaid = o.IsPaid
                    });
                    return Ok(orderspaid);
                }
                return Unauthorized();
            }
            return Unauthorized();
        }

        [HttpGet("orderscancelled")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult orderscancelled()
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var orderscancelled = unitOfWork.Generic<Orders>().GetAll().Where(oc => oc.IsDeleted == true && oc.IsPermanentDelete == true).Include(u => u.User).Select(o => new SetOrders
                    {
                        UserId = o.UserId,
                        Username = o.User.UserName,
                        OrderId = o.Id,
                        OrderDate = o.OrderDate,
                        Status = o.Status,
                        OrderItems = o.OrderItems.Select(oi => new SetOrderItems
                        {
                            Id = oi.Id,
                            MenuItemId = oi.MenuItemId,
                            MenuName = oi.MenuItem.Menu.MenuName,
                            ItemName = oi.ItemName,
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            SubTotal = oi.SubTotal
                        }).ToList(),
                        TotalPrice = o.OrderItems.Sum(res => res.SubTotal)
                    });
                    return Ok(orderscancelled);
                }
                return Unauthorized();
            }
            return Unauthorized();
        }

        [HttpGet("userorderscancelled")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult userorderscancelled(string userid)
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var orderscancelled = unitOfWork.Generic<Orders>().GetAll().Where(oc => oc.IsDeleted == true && oc.IsPermanentDelete == true && oc.UserId == userid).Include(u => u.User).Select(o => new SetOrders
                    {
                        UserId = o.UserId,
                        Username = o.User.UserName,
                        OrderId = o.Id,
                        OrderDate = o.OrderDate,
                        Status = o.Status,
                        OrderItems = o.OrderItems.Select(oi => new SetOrderItems
                        {
                            Id = oi.Id,
                            MenuItemId = oi.MenuItemId,
                            MenuName = oi.MenuItem.Menu.MenuName,
                            ItemName = oi.ItemName,
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            SubTotal = oi.SubTotal
                        }).ToList(),
                        TotalPrice = o.OrderItems.Sum(res => res.SubTotal)
                    });
                    return Ok(orderscancelled);
                }
                return Unauthorized();
            }
            return Unauthorized();
        }

        [HttpGet("reservationspaid")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult reservationspaid()
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var reservationspaid = unitOfWork.Generic<Reservation>().GetAll().Where(rp => rp.IsPaid == true).Include(u => u.User).Select(res => new SetReservation
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
                        EndDate = res.EndDate,
                        IsPaid = res.IsPaid
                    });
                    return Ok(reservationspaid);
                }
                return Unauthorized();
            }
            return Unauthorized();
        }

        [HttpGet("userreservationspaid")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult userreservationspaid(string userid)
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var reservationspaid = unitOfWork.Generic<Reservation>().GetAll().Include(u => u.User).Include(u => u.Table).Where(rp => rp.IsPaid == true && rp.UserId == userid).Select(res => new SetReservation
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
                        EndDate = res.EndDate,
                        IsPaid = res.IsPaid
                    });
                    return Ok(reservationspaid);
                }
                return Unauthorized();
            }
            return Unauthorized();
        }

        [HttpGet("reservationscancelled")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult reservationscancelled()
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var reservationscancelled = unitOfWork.Generic<Reservation>().GetAll().Where(rc => rc.IsDeleted == true && rc.IsPermanentDelete == true).Include(u => u.User).Select(res => new SetReservation
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
                        EndDate = res.EndDate,
                    });
                    return Ok(reservationscancelled);
                }
                return Unauthorized();
            }
            return Unauthorized();
        }

        [HttpGet("userreservationscancelled")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult userreservationscancelled(string userid)
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId != null)
            {
                if (RoleId == "Admin" || RoleId == "AdminAssistant")
                {
                    var reservationscancelled = unitOfWork.Generic<Reservation>().GetAll().Where(rc => rc.IsDeleted == true && rc.IsPermanentDelete == true && rc.UserId == userid).Include(u => u.User).Select(res => new SetReservation
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
                    return Ok(reservationscancelled);
                }
                return Unauthorized();
            }
            return Unauthorized();
        }


        [HttpGet("Systemdataanalysis")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> Systemdataanalysis()
        {
            var RoleId = User.FindFirst(ClaimTypes.Role)?.Value;
            if (RoleId == "Admin" || RoleId == "AdminAssistant")
            {
                var totalUsers = await user.Users.CountAsync();
                var totalCustomersQuestions = await unitOfWork.Generic<ContactUs>().GetAll().CountAsync();
                var totalcategories = await unitOfWork.Generic<Category>().GetAll().CountAsync(cat => cat.IsDeleted == false);
                var totalComplaintsAndSuggestions = await unitOfWork.Generic<ComplaintandSuggestion>().GetAll().CountAsync(cas => cas.IsDeleted == false);
                var menuitems = await unitOfWork.Generic<MenuItems>().GetAll().CountAsync(cas => cas.IsDeleted == false && cas.Menu.IsDeleted == false && cas.IsAvailable == true);
                var totalOrders = await unitOfWork.Generic<Orders>().GetAll().CountAsync(to => to.IsDeleted == false&&to.Status==OrderStatus.InProgress);
                var OrderPaid = await unitOfWork.Generic<Orders>().GetAll().CountAsync(op => op.IsDeleted == false && op.IsPaid == true);
                var OrderCancelled = await unitOfWork.Generic<Orders>().GetAll().CountAsync(oc => oc.IsDeleted == true && oc.IsPermanentDelete == true);
                var totalReservations = await unitOfWork.Generic<Reservation>().GetAll().CountAsync(tr => tr.IsDeleted == false&&tr.ReservationStatus==ReservationStatus.InProgress);
                var ReservationPaid = await unitOfWork.Generic<Reservation>().GetAll().CountAsync(rp => rp.IsDeleted == false && rp.IsPaid == true);
                var ReservationCancelled = await unitOfWork.Generic<Reservation>().GetAll().CountAsync(rc => rc.IsDeleted == true && rc.IsPermanentDelete == true);
                var ReservationFeedbacks = await unitOfWork.Generic<ReservationFeedback>().GetAll().CountAsync(rfb => rfb.IsDeleted == false);
                var OrderFeedbacks = await unitOfWork.Generic<OrderFeedback>().GetAll().CountAsync(ofb => ofb.IsDeleted == false);
                var dashboardData = new
                {
                    TotalUsers = totalUsers,
                    TotalCustomersQuestions= totalCustomersQuestions,
                    TotalCategories = totalcategories,
                    TotalComplaintsAndSuggestions = totalComplaintsAndSuggestions,
                    MenuItems = menuitems,
                    TotalOrders = totalOrders,
                    OrderPaid = OrderPaid,
                    OrderCancelled = OrderCancelled,
                    OrderFeedbacks = OrderFeedbacks,
                    TotalReservations = totalReservations,
                    ReservationPaid = ReservationPaid,
                    ReservationCancelled = ReservationCancelled,
                    ReservationFeedbacks = ReservationFeedbacks
                };
                return Ok(dashboardData);
            }
            return Unauthorized();
        }
    }
}