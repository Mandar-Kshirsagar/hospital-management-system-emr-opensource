import { ChangeDetectorRef, Component } from "@angular/core";
import * as moment from "moment";
import { SecurityService } from "../../..//security/shared/security.service";
import { NepaliCalendarService } from "../../..//shared/calendar/np/nepali-calendar.service";
import { CoreService } from "../../../core/shared/core.service";
import { CommonFunctions } from "../../../shared/common.functions";
import { MessageboxService } from "../../../shared/messagebox/messagebox.service";
import { ENUM_DanpheHTTPResponseText, ENUM_Data_Type, ENUM_DateTimeFormat, ENUM_MessageBox_Status } from "../../../shared/shared-enums";
import { CashBankBookModel, DateWiseCashBookModel } from "../../settings/shared/cash-bank-book-model";
import { Hospital_DTO } from "../../settings/shared/dto/hospitals.dto";
import { LedgerModel } from "../../settings/shared/ledger.model";
import { ledgerGroupModel } from "../../settings/shared/ledgerGroup.model";
import { AccountingService } from "../../shared/accounting.service";
import { Voucher } from "../../transactions/shared/voucher";
import { AccountingReportsBLService } from "../shared/accounting-reports.bl.service";

@Component({
  selector: 'cash-bank-book-report',
  templateUrl: './cash-bank-book-report.html',
})
export class CashBankBookReportComponent {
  public ledgerList: Array<LedgerModel> = new Array<LedgerModel>();
  public ledgerGroupList: Array<ledgerGroupModel> = new Array<ledgerGroupModel>();
  public voucherList: Array<Voucher> = new Array<Voucher>();
  public selectedLedgerGroup: any;
  public filteredLedgerList: Array<LedgerModel> = new Array<LedgerModel>()
  public selectedLedgerLists: Array<number> = [];
  public fromDate: string = null;
  public toDate: string = null;
  public fiscalYearId: number = 0;
  public validDate: boolean = false;
  public dateRange: string = '';
  public OpeningData: any;
  public TransactionData: any;
  public cashBookModelData: Array<CashBankBookModel> = new Array<CashBankBookModel>();
  public DateWiseTotal = [];
  public OriginalDateWiseTotal = [];
  public PopUpDetail = [];
  public todayDate: string = '';
  public GrandTotal: number = 0;
  public voucherNumber: string = null;
  public viewMode: string = "summary";

  public showPrint: boolean = false;
  public datePref: string = "";
  public footerContent = '';
  public printBy: string = '';
  public headerContent = '';
  public reportHeader: string = 'Cash/Bank Book Report';
  public printTitle: string = "";
  public headerDetail: any;
  public selectedLedgers: any = [];
  public ShowDetailPopUp: boolean = false;
  public HideZeroTxn: boolean = false;
  public showParticularcheckBox: boolean = false;
  public HospitalList: Array<Hospital_DTO> = new Array<Hospital_DTO>();
  public SelectedHospital: number = 0;
  public HospitalId: number = 0;
  public AllLedgerGroups: ledgerGroupModel[] = [];
  public ActiveHospital: number = 0;
  constructor(
    public accReportBLService: AccountingReportsBLService,
    public coreservice: CoreService,
    public msgBoxServ: MessageboxService,
    public accountingService: AccountingService,
    public securityService: SecurityService,
    public nepaliCalendarService: NepaliCalendarService,
    public changeDetector: ChangeDetectorRef) {
    this.LoadData();
  }
  LoadData() {
    try {
      this.todayDate = moment().format(ENUM_DateTimeFormat.Year_Month_Day);
      this.AllLedgerGroups = this.accountingService.AllLedgerGroup;
      this.ledgerList = this.accountingService.AllLedgers;
      this.GetVoucher();
      var paramValue = this.coreservice.Parameters.find(a => a.ParameterGroupName == "Common" && a.ParameterName == "CustomerHeader").ParameterValue;
      if (paramValue) {
        this.headerDetail = JSON.parse(paramValue);
      }
      this.accountingService.getCoreparameterValue();
      this.CheckAndAssignHospital();
      this.FilterLedgerGroup();
    }
    catch (error) {
      console.error('Error loading data:', error);
      this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ['Error loading data']);
    }
  }
  CheckAndAssignHospital() {
    this.ActiveHospital = this.securityService.AccHospitalInfo.ActiveHospitalId;
    this.HospitalList = this.accountingService.accCacheData.Hospitals ? this.accountingService.accCacheData.Hospitals : [];
    if (this.HospitalList.length === 1) {
      this.SelectedHospital = this.HospitalList[0].HospitalId;
    } else {
      this.SelectedHospital = this.ActiveHospital;
    }
  }

  public GetVoucher() {
    if (!!this.accountingService.accCacheData.VoucherType && this.accountingService.accCacheData.VoucherType.length > 0) {
      this.voucherList = this.accountingService.accCacheData.VoucherType;
    }
  }

  LedgerGroupListFormatter(data: any): string {
    return (
      data["LedgerGroupName"] +
      " | " +
      data["PrimaryGroup"] +
      " -> " +
      data["COA"]
    );
  }
  AssignSelectedLedgerGroup() {
    if (this.selectedLedgerGroup != null) {
      this.selectedLedgerLists = [];
      if (typeof (this.selectedLedgerGroup) == 'object' && this.selectedLedgerGroup.LedgerGroupId > 0) {
        var ledgerList = this.ledgerList.filter(a => a.LedgerGroupId == this.selectedLedgerGroup.LedgerGroupId && a.HospitalId === this.SelectedHospital);
        this.filteredLedgerList = ledgerList;
      }
      else {
        let codeDetail = this.accountingService.accCacheData.CodeDetails.filter(a => (a.Code === '021' || a.Code === '022') && a.Description === 'LedgerGroupName');
        let ledgerGroups = new Array<ledgerGroupModel>();
        if (codeDetail) {
          codeDetail.forEach(code => {
            let ledGroup = this.accountingService.accCacheData.LedgerGroups.find(a => a.Name === code.Name);
            if (ledGroup) {
              ledgerGroups.push(ledGroup)
            }
          });
        }
        this.filteredLedgerList = this.ledgerList.filter(a => ledgerGroups.some(ledGroup => a.LedgerGroupId === ledGroup.LedgerGroupId));
      }
    }
  }

  OnLedgerSelected($event) {
    if ($event) {
      this.selectedLedgerLists = [];
      if ($event && $event.length) {
        $event.forEach(v => {
          this.selectedLedgerLists.push(v.LedgerId);
        })
      }
    }
  }
  selectDate(event) {
    if (event) {
      this.fromDate = event.fromDate;
      this.toDate = event.toDate;
      this.fiscalYearId = event.fiscalYearId;
      this.validDate = true;
      this.dateRange = "&nbsp;" + this.fromDate + "&nbsp;<b>To</b>&nbsp;" + this.toDate;
    }
    else {
      this.validDate = false;
    }
  }
  GetTxnList() {
    this.cashBookModelData = [];
    this.DateWiseTotal = [];
    this.OriginalDateWiseTotal = [];
    this.PopUpDetail = [];
    this.GrandTotal = 0;
    this.HospitalId = this.SelectedHospital;
    if (this.CheckSelLedger() && this.checkDateValidation()) {
      this.accReportBLService.GetCashBankBookReport(this.fromDate, this.toDate, this.fiscalYearId, this.selectedLedgerLists, this.HospitalId)
        .subscribe(res => {
          if (res.Status === ENUM_DanpheHTTPResponseText.OK && res.Results) {
            this.OpeningData = res.Results.OpeningData;
            this.TransactionData = res.Results.TransactionData;
            this.ProcessTransactionData();
          }
          else {
            this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Notice, ["There is no data avalilable for selected date range."]);
          }
        },
          err => {
            this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["Unable to load data."]);
          });
    }

  }
  checkDateValidation() {
    let flag = true;
    if (!this.validDate) {
      this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Warning, ['Invalid date is selected. Please select valid date.']);
      flag = false;
    }
    if (!this.HospitalId) {
      this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Warning, ['Please select Account Section']);
      flag = false;
    }
    return flag;
  }
  CheckSelLedger(): boolean {
    if (!this.selectedLedgerLists || typeof (this.selectedLedgerLists) !== ENUM_Data_Type.Object || this.selectedLedgerLists.length < 1) {
      this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Warning, ["Please select at least one ledger."]);
      return false;
    }
    else
      return true;
  }

  ViewTransactionDetails(voucherNumber: string) {
    this.voucherNumber = null;
    this.changeDetector.detectChanges();
    this.voucherNumber = voucherNumber;
  }

  ProcessTransactionData() {
    this.TransactionData.sort(function (a, b) {
      if (a.TransactionDate > b.TransactionDate) return 1;
      if (a.TransactionDate < b.TransactionDate) return -1;
      return 0;
    });
    this.TransactionData.forEach((txn, index) => {
      txn.LedgerName = this.ledgerList.filter(a => a.LedgerId == txn.LedgerId)[0].LedgerName;
      //txn.VoucherType = this.voucherList.filter(a => a.VoucherId == txn.VoucherId)[0].VoucherName;
      if (index == 0)
        txn.Accumulated = this.OpeningData[0].OpeningBalance + txn.DrAmount - txn.CrAmount;
      else
        txn.Accumulated = this.TransactionData[index - 1].Accumulated + txn.DrAmount - txn.CrAmount;
    })
    let ledgerData = this.TransactionData;
    const fromDate = moment(this.fromDate);
    const toDate = moment(this.toDate).add(1, 'day');
    while (fromDate.isBefore(toDate, 'day')) {
      let todayTxnData = ledgerData.filter(a => fromDate.isSame(moment(a.TransactionDate)));
      let model = new DateWiseCashBookModel();
      model.Transactions = todayTxnData;
      if (fromDate.isSame(moment(this.fromDate))) {
        model.OpeningBalance = this.OpeningData[0].OpeningBalance;
      }
      else {
        model.OpeningBalance = this.DateWiseTotal[this.DateWiseTotal.length - 1].ClosingBalance;
      }
      model.ClosingBalance = model.OpeningBalance;
      let dr = 0;
      let cr = 0;
      model.Transactions.forEach(txn => {
        dr += txn.DrAmount;
        cr += txn.CrAmount;
        model.ClosingBalance += (txn.DrAmount - txn.CrAmount);
      })
      model.DrAmount = dr;
      model.CrAmount = cr;
      model.TransactionDate = this.nepaliCalendarService.ConvertEngToNepDateString(fromDate.toString())
      this.DateWiseTotal.push(model);
      fromDate.add(1, 'days');
    }
    this.OriginalDateWiseTotal = this.DateWiseTotal;
    this.HandleHideZeroTxn();
  }

  getNumber(num: number) {
    return Math.abs(num);
  }

  OpenDetailPopUp(row: any) {
    this.voucherNumber = null;
    this.PopUpDetail = this.DateWiseTotal.filter(a => a.TransactionDate == row.TransactionDate);
    this.ShowDetailPopUp = true;
  }

  CloseDetailPopUP() {
    this.ShowDetailPopUp = false;
  }

  HandleChangeDetailView() {
    this.voucherNumber = null;
  }

  HandleHideZeroTxn() {
    if (this.HideZeroTxn) {
      let data = this.DateWiseTotal.filter(a => a.Transactions.length != 0);
      this.DateWiseTotal = data;
    }
    else {
      this.DateWiseTotal = this.OriginalDateWiseTotal;
    }
  }

  Print(tableId) {
    let date = JSON.parse(JSON.stringify(this.dateRange));
    var printDate = moment().format(ENUM_DateTimeFormat.Year_Month_Day_Hour_Minute);//Take Current Date/Time for PrintedOn Value.
    this.printBy = this.securityService.loggedInUser.Employee.FullName;
    let printBy = JSON.parse(JSON.stringify(this.printBy));
    let popupWinindow;
    if (this.accountingService.paramData) {
      if (!this.printBy.includes("Printed")) {
        var currDate = moment().format(ENUM_DateTimeFormat.Year_Month_Day_Hour_Minute);
        var nepCurrDate = NepaliCalendarService.ConvertEngToNepaliFormatted_static(currDate, ENUM_DateTimeFormat.Year_Month_Day_Hour_Minute);
        var printedBy = (this.accountingService.paramData.ShowPrintBy) ? "<b>Printed By:</b>&nbsp;" + this.printBy : '';
        this.printBy = printedBy;
      }
      this.dateRange = (this.accountingService.paramData.ShowDateRange) ? date : date = '';
      var Header = document.getElementById("headerForPrint").innerHTML;
      var printContents = `<div>
                            <p class='alignleft'>${this.reportHeader}</p>
                            <p class='alignleft'><b>For the period:</b>
                            ${this.dateRange}<br/></p>
                            <p class='alignright'>
                              ${this.printBy}<br /> 
                              <b>Printed On:</b> (AD)${printDate}<br /> 
                            </p>
                          </div>`
      printContents += "<style> table { border-collapse: collapse; border-color: black;font-size: 11px; background-color: none; } th { color:black; background-color: #599be0;}.ADBS_btn {display:none;padding:0px ;} .tr { color:black; background-color: none;} "
      printContents += ".alignleft {float:left;width:33.33333%;text-align:left;}.aligncenter {float: left;width:33.33333%;text-align:center;}.alignright {float: left;width:33.33333%;text-align:right;}​</style>";

      printContents += document.getElementById(tableId).innerHTML
      popupWinindow = window.open(
        "",
        "_blank",
        "width=600,height=700,scrollbars=no,menubar=no,toolbar=no,location=no,status=no,titlebar=no, ADBS_btn=null"
      );
      popupWinindow.document.open();
      let documentContent = "<html><head>";
      //documentContent += '<link rel="stylesheet" type="text/css" media="print" href="../../../themes/theme-default//DanphePrintStyle.css"/>';
      documentContent +=
        '<link rel="stylesheet" type="text/css" href="../../../assets/global/plugins/bootstrap/css/bootstrap.min.css"/>';
      documentContent +=
        '<link rel="stylesheet" type="text/css" href="../../../themes/theme-default//DanpheStyle.css"/>';
      documentContent += "</head>";
      if (this.accountingService.paramData) {
        this.printTitle = this.accountingService.paramData.HeaderTitle;
        this.headerContent = Header;
        printContents = (this.accountingService.paramData.ShowHeader) ? this.headerContent + printContents : printContents;
        printContents = (this.accountingService.paramData.ShowFooter) ? printContents + this.footerContent : printContents;
      }
      documentContent +=
        '<body onload="window.print()">' + printContents + "</body></html>";
      popupWinindow.document.write(documentContent);
      popupWinindow.document.close();
    }
  }
  // Export report to Excelsheet
  public ExportToExcel(tableId) {
    try {
      let Footer = JSON.parse(JSON.stringify(this.footerContent));
      let date = JSON.parse(JSON.stringify(this.dateRange));
      date = date.replace("To", " To:");
      this.printBy = this.securityService.loggedInUser.Employee.FullName;
      let printBy = JSON.parse(JSON.stringify(this.printBy));
      let printByMessage = '';
      var hospitalName;
      var address;
      let filename;
      let workSheetName;
      filename = workSheetName = this.accountingService.paramExportToExcelData.HeaderTitle;
      if (!!this.accountingService.paramExportToExcelData) {
        if (!!this.accountingService.paramExportToExcelData.HeaderTitle) {
          if (this.accountingService.paramExportToExcelData.HeaderTitle) {
            var headerTitle = "Ledger Report"
          }

          if (this.accountingService.paramExportToExcelData.ShowPrintBy) {
            if (!printBy.includes("PrintBy")) {
              printByMessage = 'Exported By:'
            }
            else {
              printByMessage = ''
            }
          }
          else {
            printBy = '';
          }
          if (!this.accountingService.paramExportToExcelData.ShowDateRange) {
            date = ""
          }
          else {
            date
          }
          //check Header
          if (this.accountingService.paramExportToExcelData.ShowHeader == true) {
            hospitalName = this.headerDetail.hospitalName;
            address = this.headerDetail.address;
          }
          else {
            hospitalName = null;
            address = null;
          }
          //check Footer
          if (!this.accountingService.paramExportToExcelData.ShowFooter) {
            Footer = null;
          }
        }
      }
      else {
        Footer = "";
        printBy = "";
        date = "";
        printByMessage = "";

      }
      CommonFunctions.ConvertHTMLTableToExcelForAccounting(tableId, workSheetName, date,
        headerTitle, filename, hospitalName, address, printByMessage, this.accountingService.paramExportToExcelData.ShowPrintBy, this.accountingService.paramExportToExcelData.ShowHeader,
        this.accountingService.paramExportToExcelData.ShowDateRange, printBy, this.accountingService.paramExportToExcelData.ShowFooter, Footer)

    } catch (ex) {
      console.log(ex);
    }
  }
  ConvertHTMLTableToExcelForAccounting(table: any, SheetName: string, date: string, TableHeading: string, filename: string, hospitalName: string, hospitalAddress: string, printByMessage: string, showPrintBy: boolean, showHeader: boolean, showDateRange: boolean, printBy: string, ShowFooter: boolean, Footer: string) {
    try {
      var printDate = moment().format(ENUM_DateTimeFormat.Year_Month_Day_Hour_Minute);
      if (table) {
        //gets tables wrapped by a div.
        var _div = document.getElementById(table).getElementsByTagName("table");
        var colCount = [];

        //pushes the number of columns of multiple table into colCount array.
        for (let i = 0; i < _div.length; i++) {
          var col = _div[i].rows[1].cells.length;
          colCount.push(col);
        }

        //get the maximum element from the colCount array.
        var maxCol = colCount.reduce(function (a, b) {
          return Math.max(a, b);
        }, 0);

        //define colspan for td.
        var span = "colspan= " + Math.trunc(maxCol / 3);
        var hospName;
        var address;
        if (showHeader == true) {
          var Header = `<tr><td></td><td></td><td colspan="4" style="text-align:center;font-size:large;"><strong>${hospitalName}</strong></td></tr><br/><tr> <td></td><td></td><td colspan="4" style="text-align:center;font-size:small;"><strong>${hospitalAddress}</strong></td></tr><br/>
          <tr><td></td><td></td><td colspan="4" style="text-align:center;font-size:small;width:600px;"><strong>${TableHeading}</strong></td></tr><br/>
          <tr> <td style="text-align:center;"><strong>${date}</strong></td><td></td><td></td><td></td><td></td><td style="text-align:center;"><strong>${printByMessage}${printBy}</strong></td><td><strong>Exported On: ${printDate}</strong></td></tr><br>`
        } else {
          if (date == "") { //if showdate date is false
            Header = `<tr> <td style="text-align:center;"><strong> ${printByMessage} ${printBy} </strong></td><td><strong>Exported On: ${printDate}</strong></td></tr>`;
          }
          else if (printBy == "") { // if  printby is false. 
            Header = `<tr> <td style="text-align:center;"><strong>${date}</strong></td><td><strong>Exported On: ${printDate}</strong></td></tr>`;
          }
          else { //if both are true
            Header = `<tr> <td style="text-align:center;"><strong>${date}</strong></td><td></td><td></td><td></td><td style="text-align:center;"><strong>${printByMessage}${printBy}</strong></td><td><strong>Exported On: ${printDate}</strong></td></tr><br>`;
          }

        }
        let workSheetName = (SheetName.length > 0) ? SheetName : 'Sheet';
        let fromDateNp: any;
        let toDateNp = '';
        if (this.fromDate.length > 0 && this.toDate.length > 0) {
          fromDateNp = NepaliCalendarService.ConvertEngToNepaliFormatted_static(this.fromDate, '');
          toDateNp = NepaliCalendarService.ConvertEngToNepaliFormatted_static(this.toDate, '');
        }

        if (ShowFooter == true) {
          Footer = "";
        } else {
          Footer = null;
        }
        let uri = 'data:application/vnd.ms-excel;base64,'
          , template = '<html xmlns:o="urn:schemas-microsoft-com:office:office" xmlns:x="urn:schemas-microsoft-com:office:excel" xmlns="http://www.w3.org/TR/REC-html40"><head><!--[if gte mso 9]><xml><x:ExcelWorkbook><x:ExcelWorksheets><x:ExcelWorksheet><x:Name>{worksheet}</x:Name><x:WorksheetOptions><x:DisplayGridlines/></x:WorksheetOptions></x:ExcelWorksheet></x:ExcelWorksheets></x:ExcelWorkbook></xml><![endif]--><meta http-equiv="content-type" content="text/plain; charset=UTF-8"/></head><body><table><tr>}{Header}</tr>{table}</table></body></html>'
          , base64 = function (s) { return window.btoa(decodeURIComponent(encodeURIComponent(s))) }
          , format = function (s, c) { return s.replace(/{(\w+)}/g, function (m, p) { return c[p]; }) }
        if (!table.nodeType) table = document.getElementById(table)
        var ctx = {
          worksheet: workSheetName, table: table.innerHTML, Header: Header,
          footer: Footer
        }
        var link = document.createElement('a');
        link.href = uri + base64(format(template, ctx));
        link.setAttribute('download', filename);
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
      }
    } catch (ex) {
      console.log(ex);
    }
  }
  FilterLedgerGroup() {
    this.selectedLedgerGroup = null;
    this.selectedLedgerLists = null;
    this.ledgerGroupList = [];
    this.filteredLedgerList = [];
    this.DateWiseTotal = [];
    if (this.SelectedHospital > 0) {

      let codeDetail = this.accountingService.AllCodeDetails.filter(a => (a.Code === '021' || a.Code === '022') && a.Description === 'LedgerGroupName' && a.HospitalId == this.SelectedHospital);
      let ledgerGroups = new Array<ledgerGroupModel>();
      if (codeDetail) {
        codeDetail.forEach(code => {
          let ledGroup = this.AllLedgerGroups.find(a => a.Name === code.Name && a.HospitalId === this.SelectedHospital);
          if (ledGroup) {
            ledgerGroups.push(ledGroup);
          }
        });
      }
      this.filteredLedgerList = this.ledgerList.filter(a => ledgerGroups.some(ledGroup => a.LedgerGroupId === ledGroup.LedgerGroupId && a.HospitalId == this.SelectedHospital));

      if (this.AllLedgerGroups) {
        this.ledgerGroupList = [];
        ledgerGroups.forEach(code => {
          let ledGroup = this.AllLedgerGroups.find(a => a.Name === code.Name && a.HospitalId === this.SelectedHospital);
          if (ledGroup) {
            this.ledgerGroupList.push(ledGroup)
          }
        });
      }
    }
  }
}