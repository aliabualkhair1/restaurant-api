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

namespace Restaurant.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ValidToken")]

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
        public IActionResult GetOrders()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != null )
            {
                
                    var AllOrders = unitOfWork.Generic<Orders>().GetAll().Where(O => O.IsDeleted == false&&O.UserId==userId).Include(oi => oi.OrderItems).Include(u => u.User).Select(o => new SetOrders
                    {
                        Id=o.Id,
                        UserId = o.UserId,
                        Username = o.User.UserName,
                        OrderId = o.Id,
                        OrderDate = o.OrderDate,
                        Status = o.Status,
                        OrderItems = o.OrderItems.Select(oi => new SetOrderItems
                        {
                            Id=oi.Id,
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
            return Unauthorized();
        }
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public IActionResult GetOrderById(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

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
                    OrderItems = o.OrderItems.Select(oi => new SetOrderItems
                    {
                        MenuItemId = oi.MenuItemId,
                        ItemName = oi.ItemName,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        SubTotal = oi.SubTotal
                    }).ToList(),
                    TotalPrice = o.OrderItems.Sum(res => res.SubTotal)
                })
                .FirstOrDefault();

            if (order == null)
                return NotFound();

            return Ok(order);
        }

        [HttpPost]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> CreateOrder([FromBody] AddOrderDTO O)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                if (O == null)
                {
                    return BadRequest();
                }
                var OrderItems = new List<OrderItems>();
                foreach (var items in O.OrderItems)
                {
                    var MenuItems = unitOfWork.Generic<MenuItems>().GetById(items.MenuItemId);
                    if (MenuItems == null)
                    {
                        return NotFound();
                    }
                    OrderItems.Add(new OrderItems
                    {
                        MenuItemId = items.MenuItemId,
                        ItemName = MenuItems.ItemName,
                        Price = MenuItems.Price,
                        Quantity = items.Quantity,
                        SubTotal = items.Quantity * MenuItems.Price
                    });
                    if (MenuItems.Quantity > 0)
                    {
                        MenuItems.Quantity -= items.Quantity;
                    }
                    else
                    {
                        MenuItems.Quantity = 0;
                        return NotFound("Quantity is Over Please Try again later");
                    }
                }
                var order = new Orders
                {
                    OrderItems = OrderItems,
                    UserId = userId,
                    IsDeleted = false,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending,
                    TotalPrice = OrderItems.Sum(total => total.SubTotal)
                };
                unitOfWork.Generic<Orders>().Add(order);
                await unitOfWork.Complete();
                return Ok("Order Added Successfully");
            }
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
                return NotFound("Order not found");

            var orderItem = order.OrderItems.FirstOrDefault(oi => oi.Id == OrderItemId);
            if (orderItem == null)
                return NotFound($"This order item {OrderItemId} not found in order {Id}");

            var oldMenuItem = unitOfWork.Generic<MenuItems>().GetById(orderItem.MenuItemId);
            if (oldMenuItem == null || oldMenuItem.IsDeleted)
                return NotFound("Original Menu item not found or deleted.");

            var finalMenuItemId = dto.MenuItemId ?? orderItem.MenuItemId;
            var oldQuantity = orderItem.Quantity;
            var NewQuantity = dto.Quantity ?? oldQuantity;

            if (NewQuantity < 0)
                return BadRequest("Quantity must be zero or positive.");

            var newMenuItem = (finalMenuItemId == orderItem.MenuItemId) ? oldMenuItem : unitOfWork.Generic<MenuItems>().GetById(finalMenuItemId);

            if (newMenuItem == null || newMenuItem.IsDeleted)
                return NotFound("New Menu item not found or deleted.");

            var quantityDifference = NewQuantity - oldQuantity;

            if (finalMenuItemId != orderItem.MenuItemId)
            {
                oldMenuItem.Quantity += oldQuantity;

                orderItem.Price = newMenuItem.Price;

                orderItem.MenuItemId = finalMenuItemId;

                if (NewQuantity > newMenuItem.Quantity)
                    return BadRequest($"Only {newMenuItem.Quantity} items available for the new item.");

                newMenuItem.Quantity -= NewQuantity;
            }
            else
            {
                if (quantityDifference > 0)
                {
                    if (quantityDifference > newMenuItem.Quantity)
                        return BadRequest($"Only {newMenuItem.Quantity} items available in stock.");
                }

                newMenuItem.Quantity -= quantityDifference;
            }

            orderItem.Quantity = NewQuantity;
            orderItem.SubTotal = NewQuantity * orderItem.Price;

            order.TotalPrice = order.OrderItems.Sum(x => x.SubTotal);

            await unitOfWork.Complete();

            return Ok("Order item updated successfully");
        }
        [HttpPut("SoftDelete")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                var check = unitOfWork.Generic<Orders>().GetById(id);
                if (check !=null &&check.UserId==userId)
                {
                check.IsDeleted = true;
                check.Status = OrderStatus.Cancelled;
                unitOfWork.Generic<Orders>().Update(check);
                await unitOfWork.Complete();
                return Ok("Order Item Deleted Successfully");
                }
                    return Unauthorized("You can not delete this order");
            }
        }
        [HttpPut("Restore")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> Restore(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            var check = unitOfWork.Generic<Orders>().GetById(id);
            if (check != null && check.UserId == userId)
            {
                check.IsDeleted = false;
               await unitOfWork.Complete();
                return Ok("Restord done");
            }
            return Unauthorized("You can not restore this order");

        }
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        [HttpPut("ConfirmOrder")]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            var userid=User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userid == null)
            {
                return Unauthorized();
            }
            var Status = unitOfWork.Generic<Orders>().GetById(id);
            if (Status != null&&Status.UserId==userid)
            {
            if (Status.Status != OrderStatus.Pending)
            {
                return BadRequest("Order must be Pending to confirm.");
            }
            Status.Status = OrderStatus.InProgress;
            unitOfWork.Generic<Orders>().Update(Status);
            await unitOfWork.Complete();
            return Ok($"Your order became {Status.Status} status");
            }
                return Unauthorized("You can not confirm this Order");
        }
        [HttpGet("OrderFeedBack")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> GetOrderFeedBack()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId!=null)
            {
                var OrdersFeedBack = await unitOfWork.Generic<OrderFeedback>().GetAll().Where(ofb => ofb.IsDeleted == false && ofb.UserId == userId).Include(o => o.Order).Include(u => u.User).Select(ofb => new SetOrderFeedBack
                {
                    Username = ofb.User.UserName,
                    OrderId = ofb.Order.Id,
                    Comment = ofb.Comment,
                    Rating = ofb.Rating,
                    SubmittedOn = ofb.SubmittedOn
                }).ToListAsync();
                return Ok(OrdersFeedBack);
            }
            return Unauthorized("You Can see your OrdersFeedback only");
        }
        [HttpPost("OrderFeedBack")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> CreateOrderFeedBack([FromBody] AddOrderFeedbackDTO OFB)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                var order = unitOfWork.Generic<Orders>().GetById(OFB.OrderId);
                if (order == null)
                {
                    return NotFound("Order Not Found");
                }
                if (order.Status == OrderStatus.Completed)
                {
                    var feedback = new OrderFeedback
                    {
                        UserId = userId,
                        OrderId = OFB.OrderId,
                        Comment = OFB.Comment,
                        Rating = OFB.Rating,
                        SubmittedOn = DateTime.Now,
                        IsDeleted = false
                    };
                unitOfWork.Generic<OrderFeedback>().Add(feedback);
                }
                else
                {
                    return BadRequest("You can only provide feedback for completed orders.");
                }
                await unitOfWork.Complete();
                return Ok("Order Feedback Added Successfully");
            }

        }
        [HttpPatch("OrderFeedBack/{id:int}")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> UpdateOrderFeedBack(int id, [FromBody] UpdateOrderFeedbackDTO OFB)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized();

            var feedback = unitOfWork.Generic<OrderFeedback>().GetById(id);

            if (feedback == null || feedback.IsDeleted || feedback.UserId != userId)
            {
                if (feedback == null)
                    return NotFound("Feedback Not Found");

                return Unauthorized("You cannot update this feedback.");
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

            feedback.SubmittedOn = DateTime.Now;

            await unitOfWork.Complete();
            return Ok("Order Feedback Updated Successfully");
        }
        [HttpPut("DeleteOrderFeedBack")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> DeleteOrderFeedBack(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            else
            {
                var check = unitOfWork.Generic<OrderFeedback>().GetById(id);
                if (check != null&&check.UserId==userId)
                {
                check.IsDeleted = true;
                unitOfWork.Generic<OrderFeedback>().Update(check);
                await unitOfWork.Complete();
                return Ok("Order Feedback Deleted Successfully");
                }
                    return Unauthorized("You can not delete this feedback");
            }
        }
        [HttpPut("RestoreFeedback")]
        [Authorize(Roles = "Staff,Customer,Admin,AdminAssistant")]
        public async Task<IActionResult> RestoreFeedback(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Unauthorized();
            }
            var check = unitOfWork.Generic<OrderFeedback>().GetById(id);
            if (check != null && check.UserId == userId)
            {
                check.IsDeleted = false;
                await unitOfWork.Complete();
                return Ok("Restord done");
            }
            return Unauthorized("You can not restore this order feedback");
        }
    }
}