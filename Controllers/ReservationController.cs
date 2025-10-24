using AutoMapper;
using BLL.Interfaces;
using BLL.Repositories;
using DAL.DTOs.Auth;
using DAL.DTOs.Models.Add;
using DAL.DTOs.Models.Update;
using DAL.DTOs.SetUp;
using DAL.Entities.Enums;
using DAL.Entities.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Claims;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ValidToken")]

    public class ReservationController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapping;
        private readonly ILogger<ReservationController> ilogger;

        public ReservationController(IUnitOfWork UnitOfWork, IMapper Mapping,ILogger<ReservationController> Ilogger)
        {
            unitOfWork = UnitOfWork;
            mapping = Mapping;
            ilogger = Ilogger;
        }
        [HttpGet]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetAllReservations()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null )
            {
                
                      var AllReservations = unitOfWork.Generic<Reservation>().GetAll().Where(res=>res.IsDeleted==false&&res.UserId==userId).Select(res=>new SetReservation
                      {
                          ReservationId=res.Id,
                          Status=res.ReservationStatus,
                          UserId=res.UserId,
                          Username=res.User.UserName,
                          TableNumber=res.Table.TableNumber,
                          TableLocation=res.Table.Location,
                          NumberOfGuests=res.NumberOfGuests,
                          DateOfReservation=res.DateOfReservation,
                          StartDate=res.StartDate,
                          EndDate=res.EndDate
                      });
                    return Ok(AllReservations);
            }
            return Unauthorized();
            
        }
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetReservationById(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var reservation = unitOfWork.Generic<Reservation>()
                .GetAll()
                .Include(r => r.User)
                .Include(r => r.Table)
                .Where(r => r.Id == id && r.IsDeleted == false)
                .Select(res => new SetReservation
                {
                    ReservationId=res.Id,
                    UserId = res.UserId,
                    Username = res.User.UserName,
                    Status = res.ReservationStatus,
                    TableNumber = res.Table.TableNumber,
                    TableLocation = res.Table.Location,
                    NumberOfGuests = res.NumberOfGuests,
                    DateOfReservation = res.DateOfReservation,
                    StartDate = res.StartDate,
                    EndDate = res.EndDate
                })
                .FirstOrDefault();

            if (reservation == null)
                return NotFound();

            return Ok(reservation);
        }

        [HttpPost]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> AddReservation([FromBody] AddReservationDTO res)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                if (res == null)
                {
                    return BadRequest();
                }
                var table = unitOfWork.Generic<Tables>().GetById(res.TableId);
                if (table == null)
                {
                    return NotFound("Table Not Found");
                }
                var allreservation = unitOfWork.Generic<Reservation>().GetAll().Where(tr => tr.TableId == res.TableId).ToList();
                if (res.NumberOfGuests>table.Capacity)
                {
                    return BadRequest("You must reserve suite table with your family number");
                }
                foreach (var dor in allreservation) {
                
                if (dor.DateOfReservation==res.DateOfReservation)
                {
                        if (res.StartDate < dor.EndDate && res.EndDate > dor.StartDate)
                        {
                            return BadRequest("This date was reserved before Please chooce alternate table");
                        }
                }
                }
                var mappedRes = mapping.Map<Reservation>(res);
                mappedRes.UserId=userId;
                mappedRes.IsDeleted = false;
                mappedRes.ReservationStatus = ReservationStatus.Pending;
                unitOfWork.Generic<Reservation>().Add(mappedRes);
                await unitOfWork.Complete();
                return Ok("Your Reservation Added Successfully");
            }
        }
        [HttpPatch("{id:int}")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> UpdateReservation([FromBody] UpdateReservationDTO dto, int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var reservation = unitOfWork.Generic<Reservation>().GetById(id);

            if (reservation == null || reservation.UserId != userId || reservation.IsPaid)
                return Unauthorized("You cannot update this reservation");

            var finalGuests = dto.NumberOfGuests ?? reservation.NumberOfGuests;
            var finalDate = dto.DateOfReservation ?? reservation.DateOfReservation;
            var finalStart = dto.StartDate ?? reservation.StartDate;
            var finalEnd = dto.EndDate ?? reservation.EndDate;
            var finalTableId = dto.TableId ?? reservation.TableId;

            if (finalTableId > 0)
            {
                var table = unitOfWork.Generic<Tables>().GetById(finalTableId);

                if (table == null)
                    return NotFound("Table Not Found");

                if (finalGuests > table.Capacity)
                    return BadRequest("You must reserve a suitable table for your party size");

                var allReservations = unitOfWork.Generic<Reservation>()
                    .GetAll()
                    .Where(tr => tr.TableId == finalTableId && tr.Id != reservation.Id);

                foreach (var tr in allReservations)
                {
                    if (tr.DateOfReservation == finalDate &&
                        finalStart < tr.EndDate &&
                        finalEnd > tr.StartDate)
                    {
                        return BadRequest("This date was reserved before. Please choose alternate date");
                    }
                }
            }

            reservation.DateOfReservation = finalDate;
            reservation.StartDate = finalStart;
            reservation.EndDate = finalEnd;
            reservation.TableId = finalTableId;
            reservation.NumberOfGuests = finalGuests;

            await unitOfWork.Complete();
            return Ok("Your reservation updated successfully");
        }
        [HttpPut("SoftDelete")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                var check = unitOfWork.Generic<Reservation>().GetById(id);
                if (check != null&&check.UserId==userId)
                {
                check.IsDeleted = true;
                check.ReservationStatus = ReservationStatus.Cancelled;
                unitOfWork.Generic<Reservation>().Update(check);
                await unitOfWork.Complete();
                return Ok("Your reservation Deleted Successfully");
                }
                return Unauthorized("You can not delete this reservation");
            }
        }
        [HttpPut("Restore")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> Restore(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            var reservations = unitOfWork.Generic<Reservation>().GetById(id);

            var check = unitOfWork.Generic<Reservation>().GetById(id);
            if (check != null && check.UserId == userId)
            {
                check.IsDeleted = false;
                await unitOfWork.Complete();
                return Ok("Restord done");
            }
            return Unauthorized("You can not restore this reservation");
        }
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        [HttpPut("ConfirmReservation")]
        public async Task<IActionResult> ConfirmReservation(int id)
        {
            var userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userid == null)
            {
                return Unauthorized();
            }
            var Status = unitOfWork.Generic<Reservation>().GetById(id);
            if (Status != null&&Status.UserId==userid)
            {
            if (Status.ReservationStatus != ReservationStatus.Pending)
            {
                return BadRequest("Reservation must be Pending to confirm.");
            }
            Status.ReservationStatus = ReservationStatus.InProgress;
            unitOfWork.Generic<Reservation>().Update(Status);
            await unitOfWork.Complete();
            return Ok($"Your reservation became {Status.ReservationStatus} status");
            }
                return Unauthorized("You can not confirm this reservation");
        }
        [HttpGet("ReservationFeedBack")]
        [Authorize(Roles = "Admin,Staff,Customer,AdminAssistant")]
        public async Task<IActionResult> GetReservationFeedBack()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId !=null)
            {
                var ResFeedBack = await unitOfWork.Generic<ReservationFeedback>().GetAll().Where(ofb => ofb.IsDeleted == false && ofb.UserId == userId).Include(o => o.Reservation).Include(u => u.User).Select(ofb => new SetReservationFeedback
                {
                    UserId = ofb.UserId,
                    Username = ofb.User.UserName,
                    ReservationId = ofb.Reservation.Id,
                    Comment = ofb.Comment,
                    Rating = ofb.Rating,
                    SubmittedOn = ofb.SubmittedOn
                }).ToListAsync();
                return Ok(ResFeedBack);
            }
            return Unauthorized("You Can see your OrdersFeedback only");
        }
        [HttpPost("ReservationFeedBack")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> CreateReservationFeedBack([FromBody] AddReservationFeedbackDTO RFB)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                var res = unitOfWork.Generic<Reservation>().GetById(RFB.ReservationId);
                if (res == null)
                {
                    return NotFound("Reservation Not Found");
                }
                var now = DateTime.UtcNow;
                var reservationEnd = res.DateOfReservation.ToDateTime(res.EndDate).ToUniversalTime();
                if (reservationEnd<=now) {
                    var feedback = new ReservationFeedback
                    {
                        UserId = userId,
                        ReservationId = RFB.ReservationId,
                        Comment = RFB.Comment,
                        Rating = RFB.Rating,
                        SubmittedOn = DateTime.Now,
                        IsDeleted = false
                    };
                    unitOfWork.Generic<ReservationFeedback>().Add(feedback); 
                }
                else
                {
                    return BadRequest("You can not give feedback for this reservation.");
                }
                await unitOfWork.Complete();
                return Ok("Reservation Feedback Added Successfully");
            }

        }
        [HttpPatch("ReservationFeedBack/{id:int}")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> UpdateReservationFeedBack(int id, [FromBody] UpdateReservationFeedbackDTO RFB)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var feedback = unitOfWork.Generic<ReservationFeedback>().GetById(id);

            if (feedback == null)
            {
                return NotFound("Feedback Not Found");
            }

            if (feedback.UserId != userId)
            {
                return Unauthorized("You are not authorized to update this feedback.");
            }

            var finalComment = RFB.Comment ?? feedback.Comment;
            var finalRate = RFB.Rating ?? feedback.Rating;
            var finalreservtionid = RFB.ReservationId ?? feedback.ReservationId;

            var res = unitOfWork.Generic<Reservation>().GetById(finalreservtionid);
            if (res == null)
            {
                return NotFound("Linked Reservation Not Found");
            }

            feedback.ReservationId = finalreservtionid;
            feedback.Rating = finalRate;

            if (!string.IsNullOrWhiteSpace(RFB.Comment))
            {
                feedback.Comment = RFB.Comment;
            }
            else 
            {
                feedback.Comment = finalComment;
            }

            await unitOfWork.Complete();
            return Ok("Reservation Feedback Updated Successfully");
        }
        [HttpPut("DeleteReservationFeedBack")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> DeleteReservationFeedBack(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                var check = unitOfWork.Generic<ReservationFeedback>().GetById(id);
                if (check != null&&check.UserId==userId)
                {
                check.IsDeleted = true;
                unitOfWork.Generic<ReservationFeedback>().Update(check);
                await unitOfWork.Complete();
                return Ok("Reservation Feedback Deleted Successfully");
                }
                    return Unauthorized("You can not delete this reservation feedback");
            }
        }
        [HttpPut("RestoreFeedback")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> RestoreReservationFeedBack(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            var check = unitOfWork.Generic<ReservationFeedback>().GetById(id);
            if (check != null && check.UserId == userId)
            {
                check.IsDeleted = false;
                await unitOfWork.Complete();
                return Ok("Restord done");
            }
            return Unauthorized("You can not restore this reservation feedback");
        }
    }
}
