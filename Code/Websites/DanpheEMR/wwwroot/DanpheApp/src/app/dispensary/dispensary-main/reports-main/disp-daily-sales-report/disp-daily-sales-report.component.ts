
import { Component, OnInit } from '@angular/core';
import * as moment from 'moment/moment';
import { PharmacyBLService } from '../../../../pharmacy/shared/pharmacy.bl.service';
import PHRMGridColumns from '../../../../pharmacy/shared/phrm-grid-columns';
import PHRMReportsGridColumns from '../../../../pharmacy/shared/phrm-reports-grid-columns';
import { PHRMStoreModel } from '../../../../pharmacy/shared/phrm-store.model';
import { NepaliDateInGridColumnDetail, NepaliDateInGridParams } from '../../../../shared/danphe-grid/NepaliColGridSettingsModel';
import { GridEmitModel } from '../../../../shared/danphe-grid/grid-emit.model';
import { DLService } from '../../../../shared/dl.service';
import { MessageboxService } from '../../../../shared/messagebox/messagebox.service';
import { DispensaryService } from '../../../shared/dispensary.service';
@Component({
  selector: 'app-disp-daily-sales-report',
  templateUrl: './disp-daily-sales-report.component.html',
  styleUrls: ['./disp-daily-sales-report.component.css']
})
export class DispDailySalesReportComponent implements OnInit {

  ///Daily Sales Summary Report Columns variable
  DailySalesSummaryReportColumns: Array<any> = null;
  ///Daily Stock Summary Report Data variable
  DailySalesSummaryReportData: Array<any> = new Array<any>();

  public FromDate: string = null; public ItemName: string = "";
  public ToDate: string = null;
  public itemList: Array<any> = new Array<any>();
  public selectedItem: any;
  public itemId: number = null;
  public NepaliDateInGridSettings: NepaliDateInGridParams = new NepaliDateInGridParams();
  public currentDispensary: PHRMStoreModel;
  StoreId: number;
  dispensaryList: Array<any>;
  selectedDispensary: any;
  public loading: boolean = false;
  constructor(public pharmacyBLService: PharmacyBLService, public dlService: DLService, public _dispensaryService: DispensaryService,
    public msgBoxServ: MessageboxService) {
    this.FromDate = moment().format("YYYY-MM-DD");
    this.ToDate = moment().format("YYYY-MM-DD");
    this.DailySalesSummaryReportColumns = PHRMGridColumns.Dispensary_PHRMSalesitemList;//sud:6Sept'21: changed for LPH to remove cost price from the grid..
    this.currentDispensary = this._dispensaryService.activeDispensary;
    this.selectedDispensary = this.currentDispensary.Name;
    this.StoreId = this.currentDispensary.StoreId;
    this.GetItemsListDetails();
    this.GetActiveDispensarylist();
    this.NepaliDateInGridSettings.NepaliDateColumnList.push(new NepaliDateInGridColumnDetail("CreatedOn", false));
  }
  ngOnInit() {

  }
  GetActiveDispensarylist() {
    this._dispensaryService.GetAllDispensaryList()
      .subscribe(res => {
        if (res.Status == "OK") {
          this.dispensaryList = res.Results;
        }
      })
  }
  DispensaryListFormatter(data: any): string {
    return data["Name"];
  }
  OnDispensaryChange() {
    let dispensary = null;
    if (!this.selectedDispensary) {
      this.StoreId = null;
    }
    else if (typeof (this.selectedDispensary) == 'string') {
      dispensary = this.dispensaryList.find(a => a.Name.toLowerCase() == this.selectedDispensary.toLowerCase());
    }
    else if (typeof (this.selectedDispensary) == "object") {
      dispensary = this.selectedDispensary;
    }
    if (dispensary) {
      this.StoreId = dispensary.StoreId;
    }
    else {
      this.StoreId = null;
    }
  }


  ////Export data grid options for excel file
  gridExportOptions = {
    fileName: 'DailySalesSummary_' + moment().format('YYYY-MM-DD') + '.xls',
  };

  //////Function Call on Button Click of Report

  public GetItemsListDetails(): void {
    try {
      this.pharmacyBLService.GetItemListWithStoreId(this.StoreId)
        .subscribe(res => this.CallBackGetItemTypeList(res));
    }
    catch (exception) {
      this.ShowCatchErrMessage(exception);
    }
  }
  CallBackGetItemTypeList(res) {
    try {
      if (res.Status == 'OK') {
        if (res.Results) {
          this.itemList = new Array<any>();
          this.itemList = res.Results;
        }
      }
      else {
        err => {
          this.msgBoxServ.showMessage("failed", ['failed to get items..']);
        }
      }
    }
    catch (exception) {
      this.ShowCatchErrMessage(exception);
    }
  }
  ShowCatchErrMessage(exception) {
    if (exception) {
      let ex: Error = exception;
      console.log("Error Messsage =>  " + ex.message);
      console.log("Stack Details =>   " + ex.stack);
    }
  }
  onChangeItem($event) {
    try {
      if ($event.ItemId > 0) {
        this.itemId = this.selectedItem.ItemId;
      }
      else {
        this.itemId = 0;
      }
    }
    catch (exception) {
      this.ShowCatchErrMessage(exception);
    }
  }
  CheckProperSelectedItem() {
    try {
      if ((typeof this.selectedItem !== 'object') || (typeof this.selectedItem === "undefined") || (typeof this.selectedItem === null)) {
        this.selectedItem = null;
        this.itemId = 0;
      }
    }
    catch (exception) {
      this.ShowCatchErrMessage(exception);
    }
  }
  myItemListFormatter(data: any): string {
    let html = data["ItemName"];
    return html;
  }

  GetReportData() {
    if (this.FromDate == null || this.ToDate == null) {
      this.msgBoxServ.showMessage("Notice", ["Please provide valid date"]);
      return;
    }
    else {
      this.loading = true;
      this.pharmacyBLService.GetDailySalesSummaryReport(this.FromDate, this.ToDate, this.itemId, this.StoreId, null, null, null, null).finally(() => {
        this.loading = false;
        this.selectedItem = null;
        this.itemId = null;
      }).subscribe(res => {
        if (res.Status == 'OK') {
          ////Assign report Column from GridConstant to DailyStockSummaryReportColumns
          this.DailySalesSummaryReportData = PHRMReportsGridColumns.DailySalesSummaryReportColumns;
          ////Assign  Result to DailyStockSummaryReportData
          this.DailySalesSummaryReportData = res.Results;
          if (res.Status == 'OK' && res.Results.length == '') {
            this.msgBoxServ.showMessage("notice", ["no record found."]);
          }
        } else {
          this.msgBoxServ.showMessage("failed", [res.ErrorMessage])
        }
      });
    }
  }


  ////on click grid export button we are catching in component an event.. 
  ////and in that event we are calling the server excel export....
  OnGridExport($event: GridEmitModel) {
    this.dlService.ReadExcel("/api/PharmacyReport/ExportToExcelPHRMDailySalesSummaryReport?FromDate=" + this.FromDate + "&ToDate=" + this.ToDate + "&ItemName=" + this.ItemName)
      .map(res => res)
      .subscribe(data => {
        let blob = data;
        let a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = "DailySalesSummaryReport" + moment().format("DD-MMM-YYYY_HHmmA") + '.xls';
        document.body.appendChild(a);
        a.click();
      },

        res => this.ErrorMsg(res));
  }
  ErrorMsg(err) {
    this.msgBoxServ.showMessage("error", ["Sorry!!! Not able export the excel file."]);
    console.log(err.ErrorMessage);
  }

  OnFromToDateChange($event) {
    if ($event) {
      this.FromDate = $event.fromDate;
      this.ToDate = $event.toDate;
    }
  }
}







