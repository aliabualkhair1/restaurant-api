using AutoMapper;
using BLL.Interfaces;
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


    public class TablesController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapping;

        public TablesController(IUnitOfWork UnitOfWork, IMapper Mapping)
        {
            unitOfWork = UnitOfWork;
            mapping = Mapping;
        }
        [HttpGet]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetAllTables()
        {
            var now = DateTime.Now;
            var currentDate = DateOnly.FromDateTime(now);
            var currentTime = TimeOnly.FromDateTime(now);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var tables = unitOfWork.Generic<Tables>()
                .GetAll()
                .Include(t => t.Reservations)
                .ThenInclude(r => r.User).Where(d=>d.IsDeleted==false)
                .ToList();

            foreach (var table in tables)
            {
                var todayReservations = table.Reservations
                    .Where(r => r.DateOfReservation == currentDate)
                    .ToList();

                if (!todayReservations.Any())
                {
                    table.Status = TableStatus.Available;
                }
                else if (todayReservations.Any(r => r.StartDate <= currentTime && r.EndDate >= currentTime))
                {
                    table.Status = TableStatus.Occupied;
                }
                else if (todayReservations.Any(r =>
                         r.StartDate > currentTime &&
                         r.StartDate <= currentTime.AddHours(1)))
                {
                    table.Status = TableStatus.Reserved;
                }
                else
                {
                    table.Status = TableStatus.Available;
                }

                unitOfWork.Generic<Tables>().Update(table);
            }

            unitOfWork.Complete();

            var result = tables.Select(res => new SetTable
            {
                Id=res.Id,
                TableNumber = res.TableNumber,
                Capacity = res.Capacity,
                Location = res.Location,
                TableImage=res.TableImage,
                Status = res.Status,
                Reservations = res.Reservations.Select(r => new SetReservation
                {
                    ReservationId=r.Id,
                    UserId = r.UserId,
                    Username = r.User.UserName,
                    TableNumber = r.Table.TableNumber,
                    TableLocation = r.Table.Location,
                    NumberOfGuests = r.NumberOfGuests,
                    DateOfReservation = r.DateOfReservation,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                }).ToList()
            });

            return Ok(result);
        }
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetTableById(int id)
        {
            var now = DateTime.Now;
            var currentDate = DateOnly.FromDateTime(now);
            var currentTime = TimeOnly.FromDateTime(now);

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var table = unitOfWork.Generic<Tables>()
                .GetAll()
                .Include(t => t.Reservations)
                .ThenInclude(r => r.User).Where(d=>d.IsDeleted==false)
                .FirstOrDefault(t => t.Id == id);

            if (table == null)
                return NotFound();

            var todayReservations = table.Reservations
                .Where(r => r.DateOfReservation == currentDate)
                .ToList();

            if (!todayReservations.Any())
            {
                table.Status = TableStatus.Available;
            }
            else if (todayReservations.Any(r => r.StartDate <= currentTime && r.EndDate >= currentTime))
            {
                table.Status = TableStatus.Occupied;
            }
            else if (todayReservations.Any(r =>
                     r.StartDate > currentTime &&
                     r.StartDate <= currentTime.AddHours(1)))
            {
                table.Status = TableStatus.Reserved;
            }
            else
            {
                table.Status = TableStatus.Available;
            }

            unitOfWork.Generic<Tables>().Update(table);
            unitOfWork.Complete();
            var result = new SetTable
            {
                Id=table.Id,
                TableNumber = table.TableNumber,
                Capacity = table.Capacity,
                Location = table.Location,
                TableImage=table.TableImage,
                Status = table.Status,
                Reservations = table.Reservations.Select(r => new SetReservation
                {
                    ReservationId=r.Id,
                    UserId = r.UserId,
                    Username = r.User.UserName,
                    TableNumber = r.Table.TableNumber,
                    TableLocation = r.Table.Location,
                    NumberOfGuests = r.NumberOfGuests,
                    DateOfReservation = r.DateOfReservation,
                    StartDate = r.StartDate,
                    EndDate = r.EndDate,
                }).ToList()
            };

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> CreateTable([FromForm] AddTablesDTO table)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                if (table == null)
                {
                    return BadRequest();
                }
                var imageName = Guid.NewGuid().ToString() + Path.GetExtension(table.TableImage.FileName);
                var Folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/Images");
                Directory.CreateDirectory(Folder);
                var IMGPath = Path.Combine(Folder, imageName);
                using (var stream = new FileStream(IMGPath, FileMode.Create))
                {
                    await table.TableImage.CopyToAsync(stream);
                };
                var mappedTable = mapping.Map<Tables>(table);
                mappedTable.TableImage = imageName;
                mappedTable.IsDeleted= false;
                mappedTable.Status = TableStatus.Available;
                unitOfWork.Generic<Tables>().Add(mappedTable);
                await unitOfWork.Complete();
                return Ok(mappedTable);
            }
        }
        [HttpPatch("{id:int}")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> UpdateTable([FromForm] UpdateTablesDTO dto, int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var table = unitOfWork.Generic<Tables>().GetById(id);

            if (table == null || table.IsDeleted)
                return NotFound("This Table Not Found");

            var finalCapacity = dto.Capacity ?? table.Capacity;
            var finalLocation = dto.Location ?? table.Location;
            var finalTableNumber = dto.TableNumber ?? table.TableNumber;

            string imageName = table.TableImage;

            table.Capacity = finalCapacity;
            table.Location = finalLocation;
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
            return Ok("Table updated successfully");
        }
        [HttpPut("SoftDelete")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> DeleteTable(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                var check = unitOfWork.Generic<Tables>().GetById(id);
                if (check == null)
                {
                    return NotFound();
                }
                check.IsDeleted = true;
                await unitOfWork.Complete();
                return Ok("Table Deleted Successfully");
            }
        }
        [HttpPut("Restore")]
        [Authorize(Roles = "Admin,AdminAssistant")]
        public async Task<IActionResult> Restore(int id)
        {
            var check = unitOfWork.Generic<Tables>().GetById(id);
            if (check == null)
            {
                return NotFound("NotFound");
            }
            check.IsDeleted = false;
            await unitOfWork.Complete();
            return Ok("Restord done");
        }
    }

    }
