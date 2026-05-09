using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.Models;

public partial class Transaction
{
    public int TransactionId { get; set; }

    public int ProductId { get; set; }

    public string TransactionType { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTime? TransactionDate { get; set; }


    public string? PaymentStatus { get; set; } = "Pending";

    public int? UserId { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User? User { get; set; }
}
