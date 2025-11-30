using AutoMapper;
using BLL.Interfaces;
using DAL.DTOs.Models.Add;
using DAL.DTOs.Models.Update;
using DAL.DTOs.SetUp;
using DAL.Entities.Enums;
using DAL.Entities.Models;
using DAL.Entities.Pagination;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TablesController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapping;

        public TablesController(IUnitOfWork UnitOfWork, IMapper Mapping)
        {
            unitOfWork = UnitOfWork;
            mapping = Mapping;
        }
        private TableStatus GetTableStatus(Tables table, DateOnly currentDate, TimeSpan currentTime)
        {
            if (table.Reservations.Any(r =>
                r.DateOfReservation == currentDate &&
                currentTime >= r.StartTime &&
                currentTime < r.EndTime))
                return TableStatus.Occupied;

            if (table.Reservations.Any(r =>
                r.DateOfReservation == currentDate &&
                r.StartTime > currentTime &&
                r.StartTime <= currentTime.Add(TimeSpan.FromHours(1))))
                return TableStatus.Reserved;

            return TableStatus.Available;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTables(int PageNumber = 1, int PageSize = 20)
        {
            var now = DateTime.Now;
            var currentDate = DateOnly.FromDateTime(now);
            var currentTime = now.TimeOfDay;

            var tablesQuery = unitOfWork.Generic<Tables>()
                .GetAll()
                .Include(t => t.Reservations).ThenInclude(r => r.User)
                .Where(t => !t.IsDeleted)
                .Select(table => new SetTable
                {
                    Id = table.Id,
                    TableNumber = table.TableNumber,
                    Capacity = table.Capacity,
                    Location = table.Location,
                    TableImage = table.TableImage,
                    Status = GetTableStatus(table, currentDate, currentTime),
                    Reservations = table.Reservations.Select(r => new SetReservation
                    {
                        ReservationId = r.Id,
                        UserId = r.UserId,
                        Username = r.User.UserName,
                        TableNumber = r.Table.TableNumber,
                        TableLocation = r.Table.Location,
                        NumberOfGuests = r.NumberOfGuests,
                        DateOfReservation = r.DateOfReservation,
                        StartTime = r.StartTime,
                        EndTime = r.EndTime,
                    }).ToList()
                });

            var paginatedResult = await Pagination<SetTable>
                .CreateAsync(tablesQuery, PageNumber, PageSize);

            return Ok(paginatedResult);
        }

        [HttpGet("GetTableByTableNumber")]
        public IActionResult GetTableByTableNumber(string TableNumber)
        {
            var now = DateTime.Now;
            var currentDate = DateOnly.FromDateTime(now);
            var currentTime = now.TimeOfDay;

            var tables = unitOfWork.Generic<Tables>()
                .GetAll()
                .Include(t => t.Reservations).ThenInclude(r => r.User)
                .Where(t => !t.IsDeleted && t.TableNumber == TableNumber)
                .ToList();

            if (!tables.Any())
                return NotFound("لم يتم العثور على ترابيزات بهذا الرقم.");

            var results = new List<SetTable>();

            foreach (var table in tables)
            {
                var status = GetTableStatus(table, currentDate, currentTime);

                results.Add(new SetTable
                {
                    Id = table.Id,
                    TableNumber = table.TableNumber,
                    Capacity = table.Capacity,
                    Location = table.Location,
                    TableImage = table.TableImage,
                    Status = status,
                    Reservations = table.Reservations.Select(r => new SetReservation
                    {
                        ReservationId = r.Id,
                        UserId = r.UserId,
                        Username = r.User.UserName,
                        TableNumber = r.Table.TableNumber,
                        TableLocation = r.Table.Location,
                        NumberOfGuests = r.NumberOfGuests,
                        DateOfReservation = r.DateOfReservation,
                        StartTime = r.StartTime,
                        EndTime = r.EndTime,
                    }).ToList()
                });
            }

            return Ok(results);
        }


        [HttpPost]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> CreateTable([FromForm] AddTablesDTO table)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            if (table == null)
                return BadRequest("بيانات الترابيزة غير صالحة.");

            var imageName = Guid.NewGuid().ToString() + Path.GetExtension(table.TableImage.FileName);
            var Folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");
            Directory.CreateDirectory(Folder);

            var IMGPath = Path.Combine(Folder, imageName);
            using (var stream = new FileStream(IMGPath, FileMode.Create))
            {
                await table.TableImage.CopyToAsync(stream);
            }

            var tables = unitOfWork.Generic<Tables>().GetAll().ToList();
            foreach (var t in tables)
            {
                if (table.TableNumber == t.TableNumber)
                {
                    return BadRequest("من فضلك إختر رقم أخر للترابيزة لأنه موجود مسبقا");
                }
            }

            var mappedTable = mapping.Map<Tables>(table);
            mappedTable.TableImage = imageName;
            mappedTable.IsDeleted = false;
            mappedTable.Status = TableStatus.Available;

            unitOfWork.Generic<Tables>().Add(mappedTable);
            await unitOfWork.Complete();
            return Ok("تمت إضافة الترابيزة بنجاح.");
        }

        [HttpPatch("{id:int}")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> UpdateTable([FromForm] UpdateTablesDTO dto, int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var table = unitOfWork.Generic<Tables>().GetById(id);

            if (table == null || table.IsDeleted)
                return NotFound("لم يتم العثور على هذه الترابيزة.");

            var finalCapacity = dto.Capacity ?? table.Capacity;
            var finalLocation = dto.Location ?? table.Location;
            var finalTableNumber = dto.TableNumber ?? table.TableNumber;
            string imageName = table.TableImage;

            table.Capacity = finalCapacity;
            table.Location = finalLocation;

            var existtable = unitOfWork.Generic<Tables>().GetAll().ToList();
            foreach (var t in existtable)
            {
                if (dto.TableNumber == t.TableNumber)
                {
                    return BadRequest("من فضلك إختر رقم أخر للترابيزة لأنه موجود مسبقا");
                }
            }

            table.TableNumber = finalTableNumber;

            if (dto.TableImage != null)
            {
                if (!string.IsNullOrEmpty(table.TableImage))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images", table.TableImage);
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);
                }

                imageName = Guid.NewGuid().ToString() + Path.GetExtension(dto.TableImage.FileName);

                var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");
                Directory.CreateDirectory(folder);

                var newImagePath = Path.Combine(folder, imageName);
                using (var stream = new FileStream(newImagePath, FileMode.Create))
                {
                    await dto.TableImage.CopyToAsync(stream);
                }
            }

            table.TableImage = imageName;
            await unitOfWork.Complete();
            return Ok("تم تحديث الترابيزة بنجاح.");
        }

        [HttpPut("SoftDelete")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> DeleteTable(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var check = unitOfWork.Generic<Tables>().GetById(id);

            if (check == null)
                return NotFound("لم يتم العثور على الترابيزة.");

            check.IsDeleted = true;
            await unitOfWork.Complete();
            return Ok("تم حذف الترابيزة بنجاح .");
        }

        [HttpGet("GetAllDeletedTable")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetAllDeletedTable()
        {
            var getdeleted = unitOfWork.Generic<Tables>()
                .GetAll()
                .Where(t => t.IsDeleted == true)
                .Select(res => new SetTable
                {
                    Id = res.Id,
                    TableNumber = res.TableNumber,
                    Capacity = res.Capacity,
                    Location = res.Location,
                    TableImage = res.TableImage,
                    Status = res.Status,
                });

            return Ok(getdeleted);
        }

        [HttpGet("GetDeletedTableByTableNumber")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public IActionResult GetDeletedTableByTableNumber(string TableNumber)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var tables = unitOfWork.Generic<Tables>()
                .GetAll()
                .Include(t => t.Reservations).ThenInclude(r => r.User)
                // تم إزالة تحميل r.Table
                .Where(d => d.IsDeleted == true && d.TableNumber == TableNumber)
                .ToList();

            if (!tables.Any())
                return NotFound("لم يتم العثور على ترابيزات محذوفة بهذا الرقم.");

            var results = new List<SetTable>();

            foreach (var table in tables)
            {
                results.Add(new SetTable
                {
                    Id = table.Id,
                    TableNumber = table.TableNumber,
                    Capacity = table.Capacity,
                    Location = table.Location,
                    TableImage = table.TableImage,
                    Status = table.Status
                });
            }

            return Ok(results);
        }

        [HttpPut("Restore")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> Restore(int id)
        {
            var check = unitOfWork.Generic<Tables>().GetById(id);
            if (check == null)
                return NotFound("لم يتم العثور على الترابيزة.");

            check.IsDeleted = false;
            await unitOfWork.Complete();
            return Ok("تم استعادة الترابيزة بنجاح.");
        }
    }
}