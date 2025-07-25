﻿using System;

namespace DanpheEMR.Services.Pharmacy.DTOs.PurchaseOrder
{
    public class PurchaseOrderItems_DTO
    {
        public int PurchaseOrderItemId { get; set; }
        public int ItemId { get; set; }
        public int PurchaseOrderId { get; set; }
        public double Quantity { get; set; }
        public decimal StandardRate { get; set; }
        public double ReceivedQuantity { get; set; }
        public double PendingQuantity { get; set; }
        public decimal SubTotal { get; set; }
        public decimal DiscountPercentage { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal VATPercentage { get; set; }
        public decimal VATAmount { get; set; }
        public decimal CCChargePercentage { get; set; }
        public decimal CCChargeAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public string AuthorizedRemark { get; set; }
        public string Remarks { get; set; }
        public string POItemStatus { get; set; }
        public int? AuthorizedBy { get; set; }
        public DateTime? AuthorizedOn { get; set; }
        public int? CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool? IsCancel { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public int GenericId { get; set; }
        public decimal FreeQuantity { get; set; }
        public Boolean IsPacking { get; set; }
        public decimal? PackingQty { get; set; }
        public int? PackingTypeId { get; set; }
        public decimal? StripRate { get; set; }
    }
}
