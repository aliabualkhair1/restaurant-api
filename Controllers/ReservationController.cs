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
using System.Security.Claims;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReservationController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapping;
        private readonly ILogger<ReservationController> ilogger;

        public ReservationController(IUnitOfWork UnitOfWork, IMapper Mapping, ILogger<ReservationController> Ilogger)
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
            if (userId != null)
            {
                var AllReservations = unitOfWork.Generic<Reservation>().GetAll()
                    .Where(res => res.IsDeleted == false && res.UserId == userId)
                    .Select(res => new SetReservation
                    {
                        ReservationId = res.Id,
                        Status = res.ReservationStatus,
                        UserId = res.UserId,
                        Username = res.User.UserName,
                        TableNumber = res.Table.TableNumber,
                        TableLocation = res.Table.Location,
                        NumberOfGuests = res.NumberOfGuests,
                        DateOfReservation = res.DateOfReservation,
                        StartTime = res.StartTime,
                        EndTime = res.EndTime,
                        IsPaid = res.IsPaid,
                        IsDeleted = res.IsDeleted,
                        IsPermanentDelete = res.IsPermanentDelete
                    });
                return Ok(AllReservations);
            }
            return Unauthorized("غير مصرح لك بالوصول.");
        }

        [HttpGet("GetByDate")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetReservationByDate(DateOnly date)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var reservation = unitOfWork.Generic<Reservation>()
                .GetAll()
                .Include(r => r.User)
                .Include(r => r.Table)
                .Where(r => r.DateOfReservation == date && r.IsDeleted == false && r.UserId == userId)
                .Select(res => new SetReservation
                {
                    ReservationId = res.Id,
                    UserId = res.UserId,
                    Username = res.User.UserName,
                    Status = res.ReservationStatus,
                    TableNumber = res.Table.TableNumber,
                    TableLocation = res.Table.Location,
                    NumberOfGuests = res.NumberOfGuests,
                    DateOfReservation = res.DateOfReservation,
                    StartTime = res.StartTime,
                    EndTime = res.EndTime
                })
                .ToList();

            if (!reservation.Any())
                return NotFound("لم يتم العثور على حجوزات لهذا التاريخ.");

            return Ok(reservation);
        }

        [HttpPost]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> AddReservation([FromBody] AddReservationDTO res)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            else
            {
                if (res == null)
                {
                    return BadRequest("بيانات الحجز غير صالحة.");
                }
                var table = unitOfWork.Generic<Tables>().GetById(res.TableId);
                if (table == null)
                {
                    return NotFound("لم يتم العثور على الطاولة.");
                }
                var allreservation = unitOfWork.Generic<Reservation>().GetAll().Where(tr => tr.TableId == res.TableId).ToList();
                if (res.NumberOfGuests > table.Capacity)
                {
                    return BadRequest("يجب حجز طاولة مناسبة لعدد أفراد عائلتك/ضيوفك.");
                }
                foreach (var dor in allreservation)
                {
                    if (dor.DateOfReservation == res.DateOfReservation)
                    {
                        if (res.StartTime < dor.EndTime && res.EndTime > dor.StartTime)
                        {
                            return BadRequest("هذا الوقت محجوز مسبقاً. يرجى اختيار طاولة بديلة أو وقت آخر.");
                        }
                    }
                }
                var mappedRes = mapping.Map<Reservation>(res);
                mappedRes.UserId = userId;
                mappedRes.IsDeleted = false;
                mappedRes.ReservationStatus = ReservationStatus.Pending;
                unitOfWork.Generic<Reservation>().Add(mappedRes);
                await unitOfWork.Complete();
                return Ok("تمت إضافة حجزك بنجاح.");
            }
        }

        [HttpPatch("{id:int}")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> UpdateReservation([FromBody] UpdateReservationDTO dto, int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var reservation = unitOfWork.Generic<Reservation>().GetById(id);

            if (reservation == null || reservation.UserId != userId || reservation.IsPaid)
                return BadRequest("لا يمكنك تحديث هذا الحجز أو ربما تم دفعه.");

            var finalGuests = dto.NumberOfGuests ?? reservation.NumberOfGuests;
            var finalDate = dto.DateOfReservation ?? reservation.DateOfReservation;
            var finalStart = dto.StartTime ?? reservation.StartTime;
            var finalEnd = dto.EndTime ?? reservation.EndTime;
            var finalTableId = dto.TableId ?? reservation.TableId;

            if (finalTableId > 0)
            {
                var table = unitOfWork.Generic<Tables>().GetById(finalTableId);

                if (table == null)
                    return NotFound("لم يتم العثور على الطاولة.");

                if (finalGuests > table.Capacity)
                    return BadRequest("يجب حجز طاولة مناسبة لعدد أفراد عائلتك/ضيوفك.");

                var allReservations = unitOfWork.Generic<Reservation>()
                    .GetAll()
                    .Where(tr => tr.TableId == finalTableId && tr.Id != reservation.Id);

                foreach (var tr in allReservations)
                {
                    if (tr.DateOfReservation == finalDate &&
                        finalStart < tr.EndTime &&
                        finalEnd > tr.StartTime)
                    {
                        return BadRequest("هذا الوقت محجوز مسبقاً. يرجى اختيار تاريخ بديل.");
                    }
                }
            }

            reservation.DateOfReservation = finalDate;
            reservation.StartTime = finalStart;
            reservation.EndTime = finalEnd;
            reservation.TableId = finalTableId;
            reservation.NumberOfGuests = finalGuests;

            await unitOfWork.Complete();
            return Ok("تم تحديث حجزك بنجاح.");
        }

        [HttpPut("SoftDelete")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> DeleteReservation(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var reservation = unitOfWork.Generic<Reservation>().GetById(id);
            if (reservation == null || reservation.UserId != userId)
                return BadRequest("لا يمكنك حذف هذا الحجز.");
            else if (reservation.ReservationStatus == ReservationStatus.Completed || reservation.IsPaid == true)
                return BadRequest("لا يمكنك حذف هذا الحجز لأنه مدفوع");

            reservation.IsDeleted = true;
            reservation.DeletionDate = DateTime.Now;

            unitOfWork.Generic<Reservation>().Update(reservation);
            await unitOfWork.Complete();

            return Ok("تم حذف حجزك ويمكن استعادته خلال ساعة واحدة.");
        }

        private async Task CheckAndDeleteExpiredReservations(string userId)
        {
            var expiredReservations = unitOfWork.Generic<Reservation>()
                .GetAll()
                .Where(r => r.IsDeleted && !r.IsPermanentDelete && r.UserId == userId)
                .ToList();

            foreach (var reservation in expiredReservations)
            {
                if ((DateTime.Now - reservation.DeletionDate.Value).TotalHours >= 1)
                {
                    var table = unitOfWork.Generic<Tables>().GetById(reservation.TableId);
                    if (table != null)
                    {
                        table.Status = TableStatus.Available;
                        unitOfWork.Generic<Tables>().Update(table);
                    }

                    reservation.IsPermanentDelete = true;
                    reservation.ReservationStatus = ReservationStatus.Cancelled;
                    unitOfWork.Generic<Reservation>().Update(reservation);
                }
            }

            await unitOfWork.Complete();
        }

        [HttpGet("GetAllDeletedReservations")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> GetAllDeletedReservations()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("غير مصرح لك بالوصول.");

            await CheckAndDeleteExpiredReservations(userId);

            var AllReservations = unitOfWork.Generic<Reservation>()
                .GetAll()
                .Where(res => res.IsDeleted && !res.IsPermanentDelete && res.UserId == userId)
                .Select(res => new SetReservation
                {
                    ReservationId = res.Id,
                    Status = res.ReservationStatus,
                    UserId = res.UserId,
                    Username = res.User.UserName,
                    TableNumber = res.Table.TableNumber,
                    TableLocation = res.Table.Location,
                    NumberOfGuests = res.NumberOfGuests,
                    DateOfReservation = res.DateOfReservation,
                    StartTime = res.StartTime,
                    EndTime = res.EndTime
                }).ToList();

            return Ok(AllReservations);
        }

        [HttpGet("GetDeletedByDate")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> GetDeletedByDate(DateOnly date)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("غير مصرح لك بالوصول.");

            await CheckAndDeleteExpiredReservations(userId);

            var reservations = unitOfWork.Generic<Reservation>()
                .GetAll()
                .Include(r => r.User)
                .Include(r => r.Table)
                .Where(r => r.DateOfReservation == date && r.IsDeleted && !r.IsPermanentDelete && r.UserId == userId)
                .Select(res => new SetReservation
                {
                    ReservationId = res.Id,
                    UserId = res.UserId,
                    Username = res.User.UserName,
                    Status = res.ReservationStatus,
                    TableNumber = res.Table.TableNumber,
                    TableLocation = res.Table.Location,
                    NumberOfGuests = res.NumberOfGuests,
                    DateOfReservation = res.DateOfReservation,
                    StartTime = res.StartTime,
                    EndTime = res.EndTime
                }).ToList();

            if (!reservations.Any()) return NotFound("لم يتم العثور على حجوزات محذوفة لهذا التاريخ.");
            return Ok(reservations);
        }

        [HttpPut("Restore")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> Restore(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var reservation = unitOfWork.Generic<Reservation>().GetById(id);
            if (reservation == null || reservation.UserId != userId)
                return BadRequest("لا يمكنك استعادة هذا الحجز.");
            if (reservation.DeletionDate == null)
                return BadRequest("لا يمكنك استعادة هذا الحجز.");

            var hoursSinceDeletion = (DateTime.Now - reservation.DeletionDate.Value).TotalHours;
            var table = unitOfWork.Generic<Tables>().GetById(reservation.TableId);

            if (hoursSinceDeletion < 1)
            {
                reservation.IsDeleted = false;
                reservation.DeletionDate = null;

                if (table != null)
                {
                    table.Status = TableStatus.Reserved;
                    unitOfWork.Generic<Tables>().Update(table);
                }

                unitOfWork.Generic<Reservation>().Update(reservation);
                await unitOfWork.Complete();

                return Ok("تمت استعادة الحجز بنجاح.");
            }

            return BadRequest("لا يمكنك استعادة هذا الحجز.");
        }

        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        [HttpPut("ConfirmReservation")]
        public async Task<IActionResult> ConfirmReservation(int id)
        {
            var userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userid == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            var Status = unitOfWork.Generic<Reservation>().GetById(id);
            if (Status != null && Status.UserId == userid)
            {
                if (Status.ReservationStatus != ReservationStatus.Pending)
                {
                    return BadRequest("يجب أن يكون حالة الحجز قيد الانتظار للتأكيد.");
                }
                Status.ReservationStatus = ReservationStatus.InProgress;
                unitOfWork.Generic<Reservation>().Update(Status);
                await unitOfWork.Complete();
                return Ok($"أصبحت حالة حجزك  قيد التقدم");
            }
            return BadRequest("لا يمكنك تأكيد هذا الحجز.");
        }

        [HttpGet("ReservationFeedBack")]
        [Authorize(Roles = "Admin,Staff,Customer,AdminAssistant")]
        public async Task<IActionResult> GetReservationFeedBack()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var ResFeedBack = await unitOfWork.Generic<ReservationFeedback>().GetAll()
                    .Where(ofb => ofb.IsDeleted == false && ofb.UserId == userId)
                    .Include(o => o.Reservation)
                    .Include(u => u.User)
                    .Select(ofb => new SetReservationFeedback
                    {
                        Id = ofb.Id,
                        UserId = ofb.UserId,
                        Username = ofb.User.UserName,
                        ReservationId = ofb.Reservation.Id,
                        ReservationDate = ofb.Reservation.DateOfReservation,
                        Comment = ofb.Comment,
                        Rating = ofb.Rating,
                        SubmittedOn = ofb.SubmittedOn
                    }).ToListAsync();
                return Ok(ResFeedBack);
            }
            return Unauthorized("يمكنك رؤية ملاحظات حجوزاتك فقط.");
        }

        [HttpGet("GetReservationFeedbackByDate")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetReservationFeedbackByDate(DateOnly date)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var reservation = unitOfWork.Generic<ReservationFeedback>()
                .GetAll()
                .Include(r => r.User)
                .Where(r => r.IsDeleted == false && r.UserId == userId && (r.Reservation.DateOfReservation == date || r.SubmittedOn == date))
                .Select(ofb => new SetReservationFeedback
                {
                    Id = ofb.Id,
                    UserId = ofb.UserId,
                    Username = ofb.User.UserName,
                    ReservationId = ofb.Reservation.Id,
                    Comment = ofb.Comment,
                    Rating = ofb.Rating,
                    SubmittedOn = ofb.SubmittedOn
                }).ToList();

            if (!reservation.Any())
                return NotFound("لم يتم العثور على ملاحظات لحجوزاتك فى هذا التاريخ.");

            return Ok(reservation);
        }

        [HttpPost("ReservationFeedBack")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> CreateReservationFeedBack([FromBody] AddReservationFeedbackDTO RFB)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            else
            {
                var res = unitOfWork.Generic<Reservation>().GetById(RFB.ReservationId);
                if (res == null)
                {
                    return NotFound("لم يتم العثور على الحجز.");
                }
                var now = DateTime.UtcNow;
                var reservationEnd = res.DateOfReservation.ToDateTime(TimeOnly.FromTimeSpan(res.EndTime)).ToUniversalTime();
                if (reservationEnd <= now)
                {
                    var feedback = new ReservationFeedback
                    {
                        UserId = userId,
                        ReservationId = RFB.ReservationId,
                        Comment = RFB.Comment,
                        Rating = RFB.Rating,
                        SubmittedOn = DateOnly.FromDateTime(DateTime.Now),
                        IsDeleted = false
                    };
                    if (string.IsNullOrWhiteSpace(feedback.Comment))
                    {
                        return BadRequest("حقل التعليق مطلوب.");
                    }
                    unitOfWork.Generic<ReservationFeedback>().Add(feedback);
                }
                else
                {
                    return BadRequest("لا يمكنك تقديم ملاحظة لهذا الحجز قبل انتهائه.");
                }
                await unitOfWork.Complete();
                return Ok("تمت إضافة ملاحظتك حول الحجز بنجاح.");
            }
        }

        [HttpPatch("ReservationFeedBack/{id:int}")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> UpdateReservationFeedBack(int id, [FromBody] UpdateReservationFeedbackDTO RFB)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var feedback = unitOfWork.Generic<ReservationFeedback>().GetById(id);

            if (feedback == null)
            {
                return NotFound("لم يتم العثور على هذه الملاحظة.");
            }

            if (feedback.UserId != userId)
            {
                return Unauthorized("غير مصرح لك بتحديث هذه الملاحظة.");
            }

            var finalComment = RFB.Comment ?? feedback.Comment;
            var finalRate = RFB.Rating ?? feedback.Rating;
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
            return Ok("تم تحديث ملاحظتك حول الحجز بنجاح.");
        }

        [HttpPut("DeleteReservationFeedBack")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> DeleteReservationFeedBack(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            else
            {
                var check = unitOfWork.Generic<ReservationFeedback>().GetById(id);
                if (check != null && check.UserId == userId)
                {
                    check.IsDeleted = true;
                    unitOfWork.Generic<ReservationFeedback>().Update(check);
                    await unitOfWork.Complete();
                    return Ok("تم حذف ملاحظتك حول الحجز بنجاح.");
                }
                return BadRequest("لا يمكنك حذف هذه الملاحظة.");
            }
        }

        [HttpGet("GetDeletedReservationFeedBack")]
        [Authorize(Roles = "Admin,Staff,Customer,AdminAssistant")]
        public async Task<IActionResult> GetDeletedReservationFeedBack()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var ResFeedBack = await unitOfWork.Generic<ReservationFeedback>().GetAll()
                    .Where(ofb => ofb.IsDeleted == true && ofb.UserId == userId)
                    .Include(o => o.Reservation)
                    .Include(u => u.User)
                    .Select(ofb => new SetReservationFeedback
                    {
                        Id = ofb.Id,
                        UserId = ofb.UserId,
                        Username = ofb.User.UserName,
                        ReservationDate = ofb.Reservation.DateOfReservation,
                        ReservationId = ofb.Reservation.Id,
                        Comment = ofb.Comment,
                        Rating = ofb.Rating,
                        SubmittedOn = ofb.SubmittedOn
                    }).ToListAsync();
                return Ok(ResFeedBack);
            }
            return Unauthorized("يمكنك رؤية ملاحظات حجوزاتك المحذوفة فقط.");
        }

        [HttpGet("GetDeletedReservationFeedbackByDate")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetDeletedReservationFeedbackByDate(DateOnly date)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("غير مصرح لك بالوصول.");

            var reservation = unitOfWork.Generic<ReservationFeedback>()
                .GetAll()
                .Include(r => r.User)
                .Where(r => r.IsDeleted == true && r.UserId == userId && (r.Reservation.DateOfReservation == date || r.SubmittedOn == date))
                .Select(ofb => new SetReservationFeedback
                {
                    Id = ofb.Id,
                    UserId = ofb.UserId,
                    Username = ofb.User.UserName,
                    ReservationId = ofb.Reservation.Id,
                    ReservationDate = ofb.Reservation.DateOfReservation,
                    Comment = ofb.Comment,
                    Rating = ofb.Rating,
                    SubmittedOn = ofb.SubmittedOn
                }).ToList();

            if (!reservation.Any())
                return NotFound("لم يتم العثور على ملاحظات محذوفة لهذا التاريخ.");

            return Ok(reservation);
        }

        [HttpPut("RestoreFeedback")]
        [Authorize(Roles = "Admin,Customer,Staff,AdminAssistant")]
        public async Task<IActionResult> RestoreReservationFeedBack(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            var check = unitOfWork.Generic<ReservationFeedback>().GetById(id);
            if (check != null && check.UserId == userId)
            {
                check.IsDeleted = false;
                await unitOfWork.Complete();
                return Ok("تمت استعادة ملاحظة الحجز بنجاح.");
            }
            return BadRequest("لا يمكنك استعادة هذه الملاحظة.");
        }
    }
}
