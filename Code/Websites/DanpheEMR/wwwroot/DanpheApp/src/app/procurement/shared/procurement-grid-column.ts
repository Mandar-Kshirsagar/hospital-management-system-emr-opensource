import * as moment from "moment/moment";
import VerificationGridColumns from "../../verification/shared/verification-grid-column";

export default class ProcurementGridColumns {

  static PurchaseRequestList = [
    { headerName: "PR No.", field: "PRNumber", width: 30 },
    {
      headerName: "Request Date",
      width: 100,
      cellRenderer: ProcurementGridColumns.AllDateFormatRenderer,
      cellRendererParams: {
        dateFieldName: 'RequestDate'
      }
    },
    { headerName: "Vendor", field: "VendorName", width: 100 },
    { headerName: "Status", field: "RequestStatus", width: 60 },
    { headerName: "Verification Status", width: 100, cellRenderer: VerificationGridColumns.VerificationStatusRenderer },
    { headerName: "RequestedBy", field: "RequestedByName", width: 100 },
    { headerName: "PO Created", field: "IsPOCreated", width: 60, cellRenderer: VerificationGridColumns.YesNoViewerforPurchaseRequest },
    {
      headerName: "Actions",
      field: "",
      width: 150,
      cellRenderer: ProcurementGridColumns.GetPOActions,
    },
  ];

  //displays date and time in hour:minute
  static DateTimeRendererForPurchaseRequest(params) {
    return moment(params.data.RequestDate).format("YYYY-MM-DD HH:mm");
  }

  static AllDateFormatRenderer(params) {
    const dateFieldName = params.colDef.cellRendererParams.dateFieldName;
    const date = params.data[dateFieldName];
    if (date != null) {
      return moment(date).format("YYYY-MM-DD");
    }
  }
  static GetPOActions(params) {
    if (params.data.IsPOCreated && params.data.RequestStatus == 'complete') {
      return `<a danphe-grid-action="view" class="grid-action">
            View </a>`
    }
    else {
      if (params.data.IsActive == true && (params.data.RequestStatus == 'active' || params.data.RequestStatus == 'pending' || params.data.RequestStatus == 'partial')) {
        return `<a danphe-grid-action="view" class="grid-action">
            View </a>
            <a danphe-grid-action="addPO" class="grid-action">
            Add Purchase Order </a>`
      }
    }
  }
  //Column Defs for Expirable Stock Component
  static ExpirableStockList = [
    { headerName: "Item Name", field: "ItemName", width: 130 },
    { headerName: "Item Type", field: "ItemType", width: 60 },
    { headerName: "Subcategory Name", field: "SubCategoryName", width: 80 },
    { headerName: "Item Code", field: "Code", width: 60 },
    { headerName: "Unit", field: "UOMName", width: 40 },
    { headerName: "Is Active", field: "IsActive", width: 40 },
    {
      headerName: "Actions",
      field: "",
      width: 100,
      ///this is used to action according to status
      cellRenderer: ProcurementGridColumns.ShowActionForExpirableStockList,
    },
  ];

  static ShowActionForExpirableStockList(params) {
    let template = `
                <a danphe-grid-action="view" class="grid-action">
                View Details
                </a>
             `;
    return template;

  }
  //columns for POLIST
  static POList = [
    { headerName: "PO No", field: "PurchaseOrderNo", width: 60 },
    { headerName: "PO Date", field: "PoDate", width: 80 },
    { headerName: "PR No", field: "PRNumber", width: 60 },
    { headerName: "Vendor Name", field: "VendorName", width: 130 },
    { headerName: "Vendor Contact", field: "VendorContact", width: 60 },
    { headerName: "Total Amount", field: "TotalAmount", width: 100 },
    { headerName: "PO Status", field: "POStatus", width: 100 },
    { headerName: "Verification Status", width: 120, cellRenderer: VerificationGridColumns.VerificationStatusRenderer },
    {
      headerName: "Actions",
      field: "",
      width: 180,
      ///this is used to action according to status
      cellRenderer: ProcurementGridColumns.ShowActionForPOList,
    },
  ];
  static PurchaseOrderDateOnlyRenderer(params) {
    let date: string = params.data.PoDate;
    return moment(date).format("YYYY-MM-DD");
  }
  static ShowActionForPOList(params) {
    if (params.data.POStatus == "active" || params.data.POStatus == "partial") {
      let template = `</a>
                <a danphe-grid-action="view" class="grid-action">
                View
             </a>

             <a  danphe-grid-action="genReceipt" class="grid-action">
                Add Good Arrival Notification</a>
             `;
      return template;
    } else {
      let template = `<a danphe-grid-action="view" class="grid-action">
                View
             </a> &nbsp;
             
             <div class="dropdown" style="display:inline-block;">
                            <button class="dropdown-toggle grid-btnCstm" type="button" data-toggle="dropdown">...
                            <span class="caret"></span></button>
                            <ul class="dropdown-menu grid-ddlCstm">
                            <li><a danphe-grid-action="CopyToPo" >Create copy from this PO</a></li>
                            
                            </ul>
                        </div>
             `;
      return template;
    }
  }
  static GRList = [
    { headerName: "GRN", field: "GoodsReceiptNo", width: 50 },
    { headerName: "GR Date", field: "GoodsReceiptDate", width: 50 },
    { headerName: "PO No.", field: "PurchaseOrderNo", width: 40 },
    { headerName: "Vendor Bill Date", field: "VendorBillDate", width: 100 },
    { headerName: "Vendor Name", field: "VendorName", width: 150 },
    { headerName: "Vendor Contact", field: "ContactNo", width: 80 },
    { headerName: "Bill No.", field: "BillNo", width: 50 },
    { headerName: "Total Amount", field: "TotalAmount", width: 90 },
    { headerName: "Pay. Mode", field: "PaymentMode", width: 60 },//sud:4May'20--needed this to distinguish between cash and credit in frontend.
    { headerName: "Remarks", field: "Remarks", width: 70 },
    { headerName: "Verification Status", width: 100, cellRenderer: VerificationGridColumns.VerificationStatusRenderer },
    {
      headerName: "Action",
      field: "",
      width: 150,
      cellRenderer: ProcurementGridColumns.GRActionRenderer
    },
  ];
  static CreatedOnDateRenderer(params) {
    let date: string = params.data.CreatedOn;
    return moment(date).format("YYYY-MM-DD");
  }
  //Procurement GR date renderer
  static GRDateTimeRenderer(params) {
    let date: string = params.data.GoodsArrivalDate;
    return moment(date).format("YYYY-MM-DD");
  }
  static GRActionRenderer(params) {
    let template = `<a danphe-grid-action="view" class="grid-action">View</a>`;
    if (params.data.IsDonation) {
      template += `<a danphe-grid-action="show-donation-detail" class="grid-action">Donation Detail</a>`;
    }
    return template;
  }

  static ReqQuotationList = [
    { headerName: "RFQ No", field: "RFQNo", width: 100 },
    {
      headerName: "Requested Date",
      field: "RequestedOn",
      width: 100,
      cellRenderer: ProcurementGridColumns.CreatedOnDateTimeRenderer,
    },
    { headerName: "Subject", field: "Subject", width: 100 },
    { headerName: "Description", field: "Description", width: 100 },
    { headerName: "Status", field: "Status", width: 80 },
    { headerName: "Action", field: "", width: 300, cellRenderer: ProcurementGridColumns.ShowActionForRFQList },

  ]
  static CreatedOnDateTimeRenderer(params) {
    return moment(params.data.RequestedOn).format("YYYY-MM-DD");
  }
  static VendorReturnedOnDateTimeRenderer(params) {
    return moment(params.data.CreatedOn).format("YYYY-MM-DD");
  }
  static ShowActionForRFQList(params) {
    if (params.data.Status == "active" || params.data.Status == "partial") {
      if (params.data.QuotationId == null) {
        let template = `<a danphe-grid-action="View" class="grid-action">RFQ Details</a>
      <a danphe-grid-action="AttachQuotationDocuments" class="grid-action">Attach Files</a>
      <a danphe-grid-action="AddQuotationDetails" class="grid-action">Add Vendor Quot.</a>
      <a danphe-grid-action="AnalyseQuotation" class="grid-action">Analyse Quotation</a>
             `;
        return template;
      }
      else {
        let template = `<a danphe-grid-action="View" class="grid-action">RFQ Details</a>
      <a danphe-grid-action="AttachQuotationDocuments" class="grid-action">Attach Files</a>
      <a danphe-grid-action="AddQuotationDetails" class="grid-action">Update Vendor Quot.</a>
      <a danphe-grid-action="AnalyseQuotation" class="grid-action">Analyse Quotation</a>
             `;
        return template;
      }

    } else {
      let template = `<a danphe-grid-action="View" class="grid-action">RFQ Details</a>
          <a danphe-grid-action="QuotationList" class="grid-action">Quotation List</a>
          <a danphe-grid-action="SelectedQuotation" class="grid-action"> Order Letter </a>
          <a danphe-grid-action="addPO" class="grid-action" style="display:none"> Add PO </a>`;
      return template;
    }
  }

  static QuotationList = [
    { headerName: "RFQ Subject", field: "Subject" },
    { headerName: "Vendor Name", field: "VendorName" },
    {
      headerName: "Created Date",
      field: "CreatedOn",
      cellRenderer: ProcurementGridColumns.CreatedOnDateTimeRenderer,
    },
    { headerName: "Status", field: "Status" },
    {
      headerName: "Action",
      field: "",
      template: `<a danphe-grid-action="view" class="grid-action">
               View
             </a>`,
    },
  ];

  static ReturnToVendorList = [
    { headerName: "Vendor Name", field: "VendorName", width: 150 },
    { headerName: "Credit Note No", field: "CreditNoteNo", width: 150 },
    { headerName: "Returned On", field: "CreatedOn", width: 150, cellRenderer: ProcurementGridColumns.VendorReturnedOnDateTimeRenderer },
    {
      headerName: "Actions",
      field: "",
      width: 180,
      template: `
                 <a danphe-grid-action="view" class="grid-action">
                    View
                 </a>`,
    },
  ];

  static VendorsList = [
    { headerName: "S.N", field: "Sno", width: 25 },
    { headerName: "Vendor Name", field: "VendorName", width: 150 },
    { headerName: "Vendor Contact", field: "ContactNo", width: 100 },
    { headerName: "GR Date", field: "GoodReceiptDate", width: 100 },
    { headerName: "Total Amount", field: "TotalAmount", width: 100 },
    {
      headerName: "Action",
      field: "",
      width: 100,
      template: '<a danphe-grid-action="view" class="grid-action">View</a>',
    },
  ];
  static OtherChargesList = [
    { headerName: "Charge Name", field: "ChargeName", width: 100 },
    { headerName: "Description", field: "Description", width: 50 },
    { headerName: "IsDefault", field: "IsDefault", width: 50 },
    {
      headerName: "Actions",
      field: "",
      width: 100,
      template: '<a danphe-grid-action="edit" class="grid-action">Edit</a>',
    },
  ];
  static PODraftList = [
    { headerName: "Vendor Name", field: "VendorName", width: 130 },
    { headerName: "Vendor Code", field: "VendorCode", width: 60 },
    { headerName: "Vendor Contact", field: "VendorContact", width: 80 },
    { headerName: "Draft Started On", field: "CreatedOn", width: 60, cellRenderer: ProcurementGridColumns.CreatedOnStartDateTimeRenderer },
    { headerName: "Draft Created By", field: "DraftCreatedBy", width: 60 },
    { headerName: "Last Updated On", field: "LastUpdateOn", width: 60, cellRenderer: ProcurementGridColumns.CreatedOnLastUpdatedTimeRenderer, },
    { headerName: "Last Update By", field: "LastUpdateBy", width: 60 },
    { headerName: "Total Amt.", field: "TotalAmount", width: 60 },
    { headerName: "Status", field: "Status", width: 60 },
    {
      headerName: "Actions",
      field: "",
      width: 250,
      ///this is used to action according to status
      cellRenderer: ProcurementGridColumns.ShowActionForPODraftList,
    },
  ];

  static ShowActionForPODraftList(params) {
    let template = ``;
    if (params.data.Status !== "Discarded") {
      template += `</a>
                <a danphe-grid-action="review" class="grid-action">
                Review/Edit
             </a>&nbsp;
             `;
    }



    template += `</a>
                <a danphe-grid-action="view" class="grid-action">
                View/Print
             </a>&nbsp;
             `;
    if (params.data.Status !== "Discarded") {

      template += `<a danphe-grid-action="CopyToPo" class="grid-action">
                Copy To PO
             </a> &nbsp;
             `;
      template += `<a danphe-grid-action="DiscardDraft" class="grid-action">
                Discard Draft
             </a> &nbsp;
             `;
    }
    return template;
  }
  static CreatedOnStartDateTimeRenderer(params) {
    return moment(params.data.CreatedOn).format("YYYY-MM-DD HH:mm");
  }
  static CreatedOnLastUpdatedTimeRenderer(params) {
    if (params.data.LastUpdateOn) {
      return moment(params.data.LastUpdateOn).format("YYYY-MM-DD HH:mm");
    }
  }
}
