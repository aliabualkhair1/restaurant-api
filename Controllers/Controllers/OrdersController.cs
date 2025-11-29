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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Eventing.Reader;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapping;

        public OrdersController(IUnitOfWork UnitOfWork, IMapper Mapping)
        {
            unitOfWork = UnitOfWork;
            mapping = Mapping;
        }
        [HttpGet]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> GetOrders()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");
            var allOrders = unitOfWork.Generic<Orders>()
    .GetAll()
    .Where(o => o.UserId == userId&&o.IsDeleted==false)
    .Include(o => o.OrderItems)
        .ThenInclude(oi => oi.MenuItem)
            .ThenInclude(mi => mi.Menu)
    .Include(o => o.User)
    .ToList();

            foreach (var order in allOrders)
            {
                if (order.OrderItems.All(x => x.IsDeleted))
                {
                    order.IsDeleted = true;
                    order.DeletionDate = DateTime.Now;
                }
                else
                { order.IsDeleted = false; }
            }

            await unitOfWork.Complete();
            var filteredOrders = allOrders.Where(o => !o.IsDeleted);
            var result = filteredOrders.Select(o => new SetOrders
            {
                UserId = o.UserId,
                Username = o.User.UserName,
                OrderId = o.Id,
                OrderDate = o.OrderDate,
                Status = o.Status,
                IsPaid = o.IsPaid,
                OrderItems = o.OrderItems
                    .Where(oi => !oi.IsDeleted)
                    .Select(oi => new SetOrderItems
                    {
                        Id = oi.Id,
                        MenuItemId = oi.MenuItemId,
                        MenuId = oi.MenuItem.MenuId,
                        MenuName = oi.MenuItem.Menu.MenuName,
                        ItemName = oi.ItemName,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        SubTotal = oi.SubTotal
                    }).ToList(),

                TotalPrice = o.OrderItems
                    .Where(i => !i.IsDeleted)
                    .Sum(i => i.SubTotal)
            });
            return Ok(result);
        }
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetOrderItemsById(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var order = unitOfWork.Generic<Orders>()
                .GetAll()
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .Where(o => o.Id == id && o.IsDeleted == false)
                .Select(o => new SetOrders
                {
                    UserId = o.UserId,
                    Username = o.User.UserName,
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    OrderItems = o.OrderItems.Where(oi=>oi.IsDeleted==false).Select(oi => new SetOrderItems
                    {
                        Id = oi.Id,
                        MenuId = oi.MenuItem.MenuId,
                        MenuName = oi.MenuItem.Menu.MenuName,
                        MenuItemId = oi.MenuItemId,
                        ItemName = oi.ItemName,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        SubTotal = oi.SubTotal
                    }).ToList(),
                    TotalPrice = o.OrderItems.Sum(res => res.SubTotal),
                    IsPaid=o.IsPaid
                })
                .ToList();

            if (order == null)
                return NotFound("لم يتم العثور على الطلب.");

            return Ok(order);
        }
        [HttpGet("GetOrderByDate")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetOrderByDate(DateOnly date)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var order = unitOfWork.Generic<Orders>()
                .GetAll()
                .Include(o => o.OrderItems)
                .Include(o => o.User)
                .Where(ofb => ofb.IsDeleted == false && ofb.UserId == userId && (ofb.OrderDate == date))
                .Select(o => new SetOrders
                {
                    UserId = o.UserId,
                    Username = o.User.UserName,
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    OrderItems = o.OrderItems.Select(oi => new SetOrderItems
                    {
                        Id = oi.Id,
                        MenuName = oi.MenuItem.Menu.MenuName,
                        MenuItemId = oi.MenuItemId,
                        ItemName = oi.ItemName,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        SubTotal = oi.SubTotal
                    }).ToList(),
                    TotalPrice = o.OrderItems.Sum(res => res.SubTotal)
                })
                .ToList();

            if (!order.Any())
                return NotFound("لم يتم العثور على طلبات في هذا التاريخ.");

            return Ok(order);
        }

        [HttpPost]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> CreateOrder([FromBody] AddOrderDTO O)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("غير مصرح لك بالوصول.");

            if (O == null) return BadRequest("الطلب فارغ.");

            var OrderItems = new List<OrderItems>();
            var messages = new List<string>();

            foreach (var items in O.OrderItems)
            {
                var MenuItems = unitOfWork.Generic<MenuItems>().GetById(items.MenuItemId);
                if (MenuItems == null) return NotFound($"لم يتم العثور على المنتج بالمعرف {items.MenuItemId}.");

                int quantityToAdd = items.Quantity;

                if (items.Quantity > MenuItems.Quantity)
                {
                    quantityToAdd = MenuItems.Quantity;
                    messages.Add($"كان {MenuItems.Quantity} فقط من {MenuItems.ItemName} متاحًا وتمت إضافته إلى طلبك.");
                    MenuItems.Quantity = 0;
                }
                else
                {
                    MenuItems.Quantity -= items.Quantity;
                }
                MenuItems.IsAvailable = MenuItems.Quantity > 0;
                if (quantityToAdd > 0)
                {
                    OrderItems.Add(new OrderItems
                    {
                        UserId = userId,
                        MenuItemId = items.MenuItemId,
                        ItemName = MenuItems.ItemName,
                        Price = MenuItems.Price,
                        Quantity = quantityToAdd,
                        SubTotal = quantityToAdd * MenuItems.Price
                    });
                }
            }
            if (!OrderItems.Any()) return BadRequest("لم تتم إضافة أي منتجات لأن الكميات المطلوبة غير متوفرة.");

            var order = new Orders
            {
                OrderItems = OrderItems,
                UserId = userId,
                IsDeleted = false,
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                Status = OrderStatus.Pending,
                TotalPrice = OrderItems.Sum(total => total.SubTotal)
            };

            unitOfWork.Generic<Orders>().Add(order);
            await unitOfWork.Complete();
            string responseMessage = "تمت إضافة الطلب بنجاح.";
            if (messages.Any()) responseMessage += " " + string.Join(" ", messages);

            return Ok(responseMessage);
        }

        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        [HttpPatch("{Id:int}/orderitem/{OrderItemId:int}")]
        public async Task<IActionResult> UpdateOrderItem(int Id, int OrderItemId, [FromBody] UpdateOrderItemsDTO dto)
        {
            var order = unitOfWork.Generic<Orders>()
                                .GetAll()
                                .Include(o => o.OrderItems)
                                .FirstOrDefault(o => o.Id == Id);

            if (order == null || order.IsDeleted)
                return NotFound("لم يتم العثور على الطلب.");

            var orderItem = order.OrderItems.FirstOrDefault(oi => oi.Id == OrderItemId);
            if (orderItem == null)
                return NotFound($"لم يتم العثور على المنتج {OrderItemId} في الطلب {Id}.");

            var currentMenuItem = unitOfWork.Generic<MenuItems>().GetById(orderItem.MenuItemId);
            if (currentMenuItem == null || currentMenuItem.IsDeleted)
                return NotFound("لم يتم العثور على المنتج لهذا العنصر في الطلب.");

            var newQuantity = dto.Quantity ?? 0;
            if (newQuantity < 1)
                return BadRequest("يجب أن تكون الكمية 1 على الأقل.");

            var quantityDifference = newQuantity - orderItem.Quantity;

            if (quantityDifference > 0)
            {
                if (currentMenuItem.Quantity == 0)
                    return BadRequest("كمية هذا المنتج نفدت حاليًا من المخزون.");

                if (quantityDifference > currentMenuItem.Quantity)
                    return BadRequest($"الكمية المتوفرة فى المخزون {currentMenuItem.Quantity} فقط");

                if (currentMenuItem.Quantity - quantityDifference <= 0)
                {
                    currentMenuItem.Quantity = 0;
                }
                else
                {
                    currentMenuItem.Quantity -= quantityDifference;
                }
            }
            else if (quantityDifference < 0)
            {
                currentMenuItem.Quantity += -quantityDifference;
                if (currentMenuItem.Quantity > 0)
                    currentMenuItem.IsAvailable = true;
            }
            var _order = unitOfWork.Generic<Orders>().GetById(orderItem.OrderId);
            if (_order.IsPaid && order.Status == OrderStatus.Completed)
            {
                return BadRequest("لا يمكنك تحديث الكمية لأن الطلب مدفوع بالفعل");
            }
            orderItem.Quantity = newQuantity;
            orderItem.SubTotal = newQuantity * orderItem.Price;
            order.TotalPrice = order.OrderItems.Sum(x => x.SubTotal);
            await unitOfWork.Complete();

            return Ok("تم تحديث كمية المنتج  فى الطلب بنجاح.");
        }
        [HttpPut("ItemSoftDelete")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> DeleteOrderItem(int itemid)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var exist = unitOfWork.Generic<OrderItems>().GetById(itemid);
            if (exist == null) return NotFound();
            var order = unitOfWork.Generic<Orders>().GetById(exist.OrderId);
            if (order.IsPaid && order.Status == OrderStatus.Completed)
            {
                return BadRequest("لا يمكنك الحذف لأن الطلب مدفوع بالفعل");
            }
            exist.IsDeleted = true;
            exist.DeletionDate = DateTime.Now;
            exist.IsPermanentDelete = false;

            unitOfWork.Generic<OrderItems>().Update(exist);
            await unitOfWork.Complete();

            return Ok("تم الحذف بنجاح ");
        }

        private async Task CheckAndDeleteExpiredItems(string userId)
        {
            var expired = unitOfWork.Generic<OrderItems>()
                .GetAll()
                .Where(o => o.IsDeleted && !o.IsPermanentDelete && o.UserId == userId)
                .ToList();

            foreach (var item in expired)
            {
                if (item.DeletionDate.HasValue && (DateTime.Now - item.DeletionDate.Value).TotalHours >= 1)
                {
                    var menuItem = unitOfWork.Generic<MenuItems>().GetById(item.MenuItemId);
                    if (menuItem != null)
                    {
                        menuItem.Quantity += item.Quantity;
                        unitOfWork.Generic<MenuItems>().Update(menuItem);
                    }

                    item.IsPermanentDelete = true;
                    unitOfWork.Generic<OrderItems>().Update(item);
                }
            }

            await unitOfWork.Complete();
        }
        [HttpGet("GetDeletedItemsByOrderId")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> GetDeletedItemsByOrderId(int orderid)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("غير مصرح لك بالوصول.");
            await CheckAndDeleteExpiredItems(userId);
            var data = unitOfWork.Generic<OrderItems>()
                .GetAll()
                .Include(oi => oi.MenuItem)
                .ThenInclude(mi => mi.Menu)
                .Where(o => o.IsDeleted && !o.IsPermanentDelete && o.UserId == userId&&o.OrderId==orderid)
                .Select(oi => new SetOrderItems
                {
                    Id = oi.Id,
                    MenuItemId = oi.MenuItemId,
                    MenuId = oi.MenuItem.MenuId,
                    MenuName = oi.MenuItem.Menu.MenuName,
                    ItemName = oi.ItemName,
                    Quantity = oi.Quantity,
                    Price = oi.Price,
                    SubTotal = oi.SubTotal
                }).ToList();
            return Ok(data);
        }

        [HttpPut("RestoreItem")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> RestoreItem(int itemid)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var orderitem = unitOfWork.Generic<OrderItems>().GetById(itemid);
            if (orderitem == null) return NotFound();

            if (!orderitem.DeletionDate.HasValue) return BadRequest();

            var hours = (DateTime.Now - orderitem.DeletionDate.Value).TotalHours;

            if (hours < 1)
            {
                orderitem.IsDeleted = false;
                orderitem.DeletionDate = null;
                unitOfWork.Generic<OrderItems>().Update(orderitem);
                await unitOfWork.Complete();
                return Ok();
            }

            var menuItem = unitOfWork.Generic<MenuItems>().GetById(orderitem.MenuItemId);
            if (menuItem != null)
            {
                menuItem.Quantity += orderitem.Quantity;
                unitOfWork.Generic<MenuItems>().Update(menuItem);
            }

            orderitem.IsPermanentDelete = true;
            unitOfWork.Generic<OrderItems>().Update(orderitem);

            await unitOfWork.Complete();

            return BadRequest();
        }


        [HttpPut("SoftDelete")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var order = unitOfWork.Generic<Orders>()
                                     .GetAll()
                                     .FirstOrDefault(o => o.Id == id && o.UserId == userId);

            if (order == null ||order.IsPaid==true||order.Status==OrderStatus.Completed)
                return BadRequest("لا يمكنك حذف هذا الطلب  لأنه مدفوع .");

            order.IsDeleted = true;
            order.DeletionDate = DateTime.Now;
            await unitOfWork.Complete();

            return Ok("تم حذف الطلب بنجاح ويمكن استعادته في غضون ساعة واحدة.");
        }
        private async Task CheckAndDeleteExpiredOrders(string userId)
        {
            var expiredOrders = unitOfWork.Generic<Orders>()
                .GetAll()
                .Where(o => o.IsDeleted && !o.IsPermanentDelete && o.UserId == userId)
                .ToList();

            foreach (var order in expiredOrders)
            {
                if (order.DeletionDate.HasValue && (DateTime.Now - order.DeletionDate.Value).TotalHours >= 1)
                {
                    var orderItems = unitOfWork.Generic<OrderItems>()
                        .GetAll()
                        .Where(oi => oi.OrderId == order.Id)
                        .ToList();

                    foreach (var item in orderItems)
                    {
                        item.IsDeleted = true;
                        item.DeletionDate = DateTime.Now;
                        item.IsPermanentDelete = true;
                        var menuItem = unitOfWork.Generic<MenuItems>().GetById(item.MenuItemId);
                        if (menuItem != null)
                        {
                            menuItem.Quantity += item.Quantity;
                            unitOfWork.Generic<MenuItems>().Update(menuItem);
                        }
                    }

                    order.IsPermanentDelete = true;
                    order.Status = OrderStatus.Cancelled;
                    unitOfWork.Generic<Orders>().Update(order);
                }
            }

            await unitOfWork.Complete();
        }

        [HttpGet("GetDeletedOrders")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> GetDeletedOrders()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("غير مصرح لك بالوصول.");

            await CheckAndDeleteExpiredOrders(userId);

            var AllOrders = unitOfWork.Generic<Orders>()
                .GetAll()
                .Where(o => o.IsDeleted && !o.IsPermanentDelete && o.UserId == userId)
                .Select(o => new SetOrders
                {
                    UserId = o.UserId,
                    Username = o.User.UserName,
                    OrderId = o.Id,
                    OrderDate = o.OrderDate,
                    Status = o.Status,
                    IsPaid = o.IsPaid,
                    OrderItems = o.OrderItems.Select(oi => new SetOrderItems
                    {
                        Id = oi.Id,
                        MenuItemId = oi.MenuItemId,
                        MenuName = oi.MenuItem.Menu.MenuName,
                        ItemName = oi.ItemName,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        SubTotal = oi.SubTotal,
                        IsDeleted=oi.IsDeleted
                    }).ToList(),
                    TotalPrice = o.OrderItems.Sum(res => res.SubTotal)
                }).ToList();

            return Ok(AllOrders);
        }

        [HttpGet("GetDeletedOrderByDate")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> GetDeletedOrderByDate(DateOnly date)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized("غير مصرح لك بالوصول.");

            await CheckAndDeleteExpiredOrders(userId);

            var orders = unitOfWork.Generic<Orders>()
                .GetAll()
                .Where(o => o.IsDeleted && !o.IsPermanentDelete && o.UserId == userId && o.OrderDate == date)
                .Select(o => new SetOrders
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
                }).ToList();

            if (!orders.Any()) return NotFound("لم يتم العثور على طلبات محذوفة في هذا التاريخ.");

            return Ok(orders);
        }
        [HttpPut("Restore")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> Restore(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var order = unitOfWork.Generic<Orders>().GetById(id);
            if (order == null)
                return NotFound("لم يتم العثور على الطلب.");

            if (order.UserId != userId)
                return Forbid("لا يمكنك استعادة هذا الطلب.");

            if (!order.DeletionDate.HasValue)
                return BadRequest("لا يمكن استعادة هذا الطلب لأن تاريخ الحذف مفقود.");

            var hoursSinceDeletion = (DateTime.Now - order.DeletionDate.Value).TotalHours;

            if (hoursSinceDeletion < 1)
            {
                order.IsDeleted = false;
                order.DeletionDate = null;
                unitOfWork.Generic<Orders>().Update(order);
                await unitOfWork.Complete();
                return Ok("تمت الاستعادة بنجاح");
            }

            var orderItems = unitOfWork.Generic<OrderItems>()
                                            .GetAll()
                                            .Where(oi => oi.OrderId == id)
                                            .ToList();

            foreach (var item in orderItems)
            {
                var menuItem = unitOfWork.Generic<MenuItems>().GetById(item.MenuItemId);
                if (menuItem != null)
                    menuItem.Quantity += item.Quantity;
                unitOfWork.Generic<MenuItems>().Update(menuItem);
            }

            unitOfWork.Generic<Orders>().Update(order);
            await unitOfWork.Complete();

            return BadRequest("لا يمكنك استعادة هذا الطلب لأنه انتهت صلاحيته.");
        }


        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        [HttpPut("ConfirmOrder")]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var userid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userid == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            var Status = unitOfWork.Generic<Orders>().GetById(id);
            if (Status != null && Status.UserId == userid)
            {
                if (Status.Status != OrderStatus.Pending)
                {
                    return BadRequest("يجب أن تكون حالة الطلب قيد الإنتظار فقط لتتمكن من تأكيده.");
                }
                Status.Status = OrderStatus.InProgress;
                unitOfWork.Generic<Orders>().Update(Status);
                await unitOfWork.Complete();
                return Ok($"أصبحت حالة طلبك قيد التقدم");
            }
            return BadRequest("لا يمكنك تأكيد هذا الطلب");
        }
        [HttpGet("OrderFeedBack")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> GetOrderFeedBack()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var OrdersFeedBack = await unitOfWork.Generic<OrderFeedback>().GetAll().Where(ofb => ofb.IsDeleted == false && ofb.UserId == userId).Include(o => o.Order).Include(u => u.User).Select(ofb => new SetOrderFeedBack
                {
                    UserId = ofb.UserId,
                    Id = ofb.Id,
                    Username = ofb.User.UserName,
                    OrderDate = ofb.Order.OrderDate,
                    OrderId = ofb.Order.Id,
                    Comment = ofb.Comment,
                    Rating = ofb.Rating,
                    SubmittedOn = ofb.SubmittedOn
                }).ToListAsync();
                return Ok(OrdersFeedBack);
            }
            return BadRequest("يمكنك رؤية ملاحظات طلباتك فقط");
        }
        [HttpGet("GetOrderFeedbackByDate")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetOrderFeedbackByDate(DateOnly date)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var orderfeedback = unitOfWork.Generic<OrderFeedback>()
                .GetAll()
                .Include(ofb => ofb.User)
                .Where(ofb => ofb.IsDeleted == false && ofb.UserId == userId && (ofb.Order.OrderDate == date || ofb.SubmittedOn == date))
                .Select(ofb => new SetOrderFeedBack
                {
                    Username = ofb.User.UserName,
                    OrderDate = ofb.Order.OrderDate,
                    OrderId = ofb.Order.Id,
                    Comment = ofb.Comment,
                    Rating = ofb.Rating,
                    SubmittedOn = ofb.SubmittedOn
                }).ToList();

            if (!orderfeedback.Any())
                return NotFound("لم يتم العثور على ملاحظات للطلب في هذا التاريخ.");

            return Ok(orderfeedback);
        }
        [HttpPost("OrderFeedBack")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> CreateOrderFeedBack([FromBody] AddOrderFeedbackDTO OFB)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            else
            {
                var order = unitOfWork.Generic<Orders>().GetById(OFB.OrderId);
                if (order == null)
                {
                    return NotFound("لم يتم العثور على الطلب");
                }
                if (order.Status == OrderStatus.Completed)
                {
                    var feedback = new OrderFeedback
                    {
                        UserId = userId,
                        OrderId = OFB.OrderId,
                        Comment = OFB.Comment,
                        Rating = OFB.Rating,
                        SubmittedOn = DateOnly.FromDateTime(DateTime.Now),
                        IsDeleted = false,
                        
                    };
                    unitOfWork.Generic<OrderFeedback>().Add(feedback);
                }
                else
                {
                    return BadRequest("يمكنك فقط تقديم ملاحظات للطلبات المدفوعة.");
                }
                await unitOfWork.Complete();
                return Ok("تمت إضافة ملاحظات الطلب بنجاح");
            }

        }
        [HttpPatch("OrderFeedBack/{id:int}")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> UpdateOrderFeedBack(int id, [FromBody] UpdateOrderFeedbackDTO OFB)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var feedback = unitOfWork.Generic<OrderFeedback>().GetById(id);

            if (feedback == null || feedback.IsDeleted || feedback.UserId != userId)
            {
                if (feedback == null)
                    return NotFound("لم يتم العثور على الملاحظات");

                return BadRequest("لا يمكنك تحديث هذه الملاحظات.");
            }

            var finalOrderId = OFB.OrderId ?? feedback.OrderId;
            var finalRating = OFB.Rating ?? feedback.Rating;
            var finalComment = OFB.Comment ?? feedback.Comment;

            feedback.OrderId = finalOrderId;
            feedback.Rating = finalRating;

            if (!string.IsNullOrWhiteSpace(OFB.Comment))
            {
                feedback.Comment = OFB.Comment;
            }
            else
            {
                feedback.Comment = finalComment;
            }
            await unitOfWork.Complete();
            return Ok("تم تحديث ملاحظات الطلب بنجاح");
        }
        [HttpPut("DeleteOrderFeedBack")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> DeleteOrderFeedBack(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            else
            {
                var check = unitOfWork.Generic<OrderFeedback>().GetById(id);
                if (check != null && check.UserId == userId)
                {
                    check.IsDeleted = true;
                    unitOfWork.Generic<OrderFeedback>().Update(check);
                    await unitOfWork.Complete();
                    return Ok("تم حذف الملاحظة الخاصة بالطلب بنجاح");
                }
                return BadRequest("لا يمكنك حذف هذه الملاحظات");
            }
        }
        [HttpGet("DeletedOrdersFeedBack")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> DeletedOrdersFeedBack()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null)
            {
                var OrdersFeedBack = await unitOfWork.Generic<OrderFeedback>().GetAll().Where(ofb => ofb.IsDeleted == true && ofb.UserId == userId).Include(o => o.Order).Include(u => u.User).Select(ofb => new SetOrderFeedBack
                {
                    Id = ofb.Id,
                    OrderDate = ofb.Order.OrderDate,
                    Comment = ofb.Comment,
                    Rating = ofb.Rating,
                    SubmittedOn = ofb.SubmittedOn
                }).ToListAsync();
                return Ok(OrdersFeedBack);
            }
            return Unauthorized("يمكنك رؤية ملاحظات طلباتك المحذوفة فقط");
        }
        [HttpGet("GetDeletedOrderFeedbackByDate")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetDeletedOrderFeedbackByDate(DateOnly date)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("غير مصرح لك بالوصول.");

            var orderfeedback = unitOfWork.Generic<OrderFeedback>()
                .GetAll()
                .Include(ofb => ofb.User)
                .Where(ofb => ofb.IsDeleted == true && ofb.UserId == userId && (ofb.Order.OrderDate == date || ofb.SubmittedOn == date))
                .Select(ofb => new SetOrderFeedBack
                {
                    UserId = ofb.UserId,
                    Username = ofb.User.UserName,
                    Id = ofb.Id,
                    OrderDate = ofb.Order.OrderDate,
                    OrderId = ofb.Order.Id,
                    Comment = ofb.Comment,
                    Rating = ofb.Rating,
                    SubmittedOn = ofb.SubmittedOn
                }).ToList();

            if (!orderfeedback.Any())
                return NotFound("لم يتم العثور على ملاحظات محذوفة للطلب في هذا التاريخ.");

            return Ok(orderfeedback);
        }
        [HttpPut("RestoreFeedback")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> RestoreFeedback(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized("غير مصرح لك بالوصول.");
            }
            var check = unitOfWork.Generic<OrderFeedback>().GetById(id);
            if (check != null && check.UserId == userId)
            {
                check.IsDeleted = false;
                await unitOfWork.Complete();
                return Ok("تمت استعادة ملاحظة الطلب بنجاح");
            }
            return BadRequest("لا يمكنك استعادة هذه الملاحظة.");
        }
    }
}