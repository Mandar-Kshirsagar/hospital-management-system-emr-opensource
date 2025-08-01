import { HttpClient, HttpHeaders } from "@angular/common/http";
import { ChangeDetectorRef, Component, EventEmitter, Input, Output } from "@angular/core";
import { Router } from "@angular/router";
import * as moment from "moment";
import { Observable, Subscription } from "rxjs";
import { BillingService } from "../../billing/shared/billing.service";
import { CoreService } from "../../core/shared/core.service";
import { SecurityService } from "../../security/shared/security.service";
import { ENUM_PrintingType, PrinterSettingsModel } from "../../settings-new/printers/printer-settings.model";
import { NepaliCalendarService } from "../../shared/calendar/np/nepali-calendar.service";
import { DanpheHTTPResponse } from "../../shared/common-models";
import { CommonFunctions } from "../../shared/common.functions";
import { MessageboxService } from "../../shared/messagebox/messagebox.service";
import { ENUM_Country, ENUM_DanpheHTTPResponseText, ENUM_DateTimeFormat, ENUM_MembershipTypeName, ENUM_MessageBox_Status, ENUM_PriceCategory, ENUM_PrintSheetTemplateCodes, ENUM_PrintSheetTemplateVisitType, ENUM_VisitType } from "../../shared/shared-enums";
import { Visit } from "../shared/patient-vist-data.dto";
import { PatientSticketViewModel } from "./patient-sticker.model";


@Component({
  selector: 'patient-sticker',
  templateUrl: "./patient-sticker.html"
})
export class PatientStickerComponent {
  @Input('patientId') public patientId: number;
  public ageSex: string = '';
  public PatientStickerDetails: PatientSticketViewModel = new PatientSticketViewModel;
  public showPatientSticker: boolean = false;
  @Output("after-print-action")
  afterPrintAction: EventEmitter<Object> = new EventEmitter<Object>();
  loading = false;
  //public Patient: Patient = new Patient();
  public currentDateTime: string;
  public localDateTime: string;
  public options = {
    headers: new HttpHeaders({ 'Content-Type': 'application/x-www-form-urlencoded' })
  };

  public showServerPrintBtn: boolean = false;
  public showLoading: boolean = false;
  public printerNameSelected: any = null;

  //for QR-specific purpose only--sud.
  public showQrCode: boolean = false;
  public patientQRCodeInfo: string = "";
  public maxFollowUpDays: number = null;
  public doctorOrDepartment: string = null;
  public EnableShowTicketPrice: boolean = false;
  public hospitalCode: string = '';
  public QueueNoSetting = { "ShowInInvoice": false, "ShowInSticker": false };
  public allPrinterName: any = null;
  public showStickerChange: boolean = false;
  public printerName: string = null;
  public showRoomNumber: boolean = false;
  public roomNo: string = null;

  public dotPrinterDimensions: any;
  public printByDotMatrixPrinter: boolean = false;
  public PrinterDisplayName: string = null;
  public PrinterNameDotMatix: string = null;
  public ModelName: string = null;
  public showPrinterChange: boolean = false;
  public billingDotMatrixPrinters: Array<any>;
  public serverPrintFolderPath: any;
  public openBrowserPrintWindow: boolean = false;
  public browserPrintContentObj: any;
  public defaultFocus: string = null;

  public closePopUpAfterStickerPrint: boolean = true;
  public PrintedBy: string;
  public showMunicipality: boolean;
  public CountryNepal: string = null;
  public SSFPriceCategoryName: string = (ENUM_PriceCategory.SSF).toUpperCase();
  public ECHSMembershipTypeName: string = ENUM_MembershipTypeName.ECHS;
  IsEnglishCalendarType: boolean = false;
  public PrintSheetTemplate: string = '';
  public PatientVisitDetails: Visit = null;


  constructor(
    public http: HttpClient,
    public _msgBoxServ: MessageboxService,
    public router: Router,
    public nepaliCalendarServ: NepaliCalendarService,
    public coreService: CoreService,
    public changeDetector: ChangeDetectorRef,
    public billingService: BillingService,
    public securityService: SecurityService
  ) {
    this.IsEnglishCalendarType = this.coreService.IsEnableEnglishCalendarOnly();
    this.showHidePrintButton();

    this.hospitalCode = this.coreService.GetHospitalCode();

    this.hospitalCode = (this.hospitalCode && this.hospitalCode.trim().length > 0) ? this.hospitalCode : "allhosp";

    this.printerName = localStorage.getItem('Danphe_OPD_Default_PrinterName');
    var allStickerFolderDetail = this.coreService.Parameters.find(a => a.ParameterGroupName.toLowerCase() === 'reg-sticker' && a.ParameterName === 'StickerPrinterSettings');
    if (allStickerFolderDetail) {
      this.allPrinterName = JSON.parse(allStickerFolderDetail.ParameterValue);
    }

    this.SetPrinterFromParam();
    this.showMunicipality = this.coreService.ShowMunicipality().ShowMunicipality;
    this.CountryNepal = ENUM_Country.Nepal;
  }

  ngOnInit() {
    this.GetPatientStickerDetails(this.patientId);
    this.LoadPatientLatestVistDetails(this.patientId);
    this.PrintedBy = this.securityService.GetLoggedInUser().UserName;
  }

  ngAfterViewInit() {
    //this.SetFocusById("btnPrintSticker");
    let btnObj = document.getElementById('btnPrintOpdSticker');
    if (btnObj && this.defaultFocus.toLowerCase() === "sticker") {
      btnObj.focus();
    }

  }

  GetPatientStickerDetails(PatientId: number) {
    this.http.get<any>('/api/Stickers/GetPatientStickerDetails?PatientId=' + PatientId, this.options)
      .map(res => res)
      .subscribe(res => this.CallBackStickerOnly(res),
        res => this.Error(res));
  }
  CallBackStickerOnly(res: any) {
    if (res.Status === ENUM_DanpheHTTPResponseText.OK && res.Results && res.Results.length !== 0) {
      this.PatientStickerDetails = { ...res.Results };
      this.PatientStickerDetails.MunicipalityName = ((res.Results.MunicipalityName !== null) || (res.Results.MunicipalityName !== "")) ? res.Results.MunicipalityName : "";
      this.PatientStickerDetails.CountrySubDivisionName = ((res.Results.CountrySubDivisionName !== null || res.Results.CountrySubDivisionName !== "")) ? res.Results.CountrySubDivisionName : "";
      if (!this.IsEnglishCalendarType) {
        this.localDateTime = this.GetLocalDate() + "BS";
      } else {
        this.localDateTime = new Date().toLocaleDateString('en-GB');
      }
      this.ageSex = CommonFunctions.GetFormattedAgeSexforSticker(this.PatientStickerDetails.DateOfBirth, this.PatientStickerDetails.Gender, this.PatientStickerDetails.Age);
      this.patientQRCodeInfo = `Name: ` + this.PatientStickerDetails.PatientName + `
  Hospital No: `+ this.PatientStickerDetails.HospitalNo + `
  Age/Sex: `+ this.PatientStickerDetails.Age + `
  Contact No: `+ this.PatientStickerDetails.Contact + `
  Address: `+ this.PatientStickerDetails.Address + `
  PrintedBy: ` + this.PrintedBy + `
  PrintedOn: ` + this.localDateTime
      //set this to true only after all values are set.
      //this.showQrCode = true;
      this.showPatientSticker = true;
    }
    else {
      this.showPatientSticker = false;
      this.AfterPrintAction();
      this._msgBoxServ.showMessage("error", ["Sorry!!! not able to get date for opd-sticker of this patient"]);
    }
  }

  printStickerDotMatrix() {
    if (this.printByDotMatrixPrinter) {
      this.PrintDotMatrix();
      return;
    }
  }

  printStickerClient() {

    let popupWinindow;
    var printContents = document.getElementById("OPDsticker").innerHTML;
    popupWinindow = window.open('', '_blank', 'width=600,height=700,scrollbars=no,menubar=no,toolbar=no,location=no,status=no,titlebar=no');
    popupWinindow.document.open();
    let documentContent = '<html><head>';
    documentContent += `<style>
        .opdstickercontainer {
        width: 370px;
        margin: 0px;
        display: block;
        font-size: 13px;
      }
  
      .stkrtopsection {
        width: 100%;
      }
  
     .dptdesc-left {
        width: 80%;
        display: inline-block;
        margin - top: 5px
      }
  
      .opd-qrcode {
        width: 15%;
        display: inline-block;
        vertical-align: top;
        float: right;
        margin: 8px 15px 0 0;
      }
      </style>`;
    documentContent += '<link rel="stylesheet" type="text/css" href="../../themes/theme-default/DanphePrintStyle.css"/>';
    /// documentContent += '<link rel="stylesheet" type="text/css" href="../../../assets/global/plugins/bootstrap/css/bootstrap.min.css"/>';
    documentContent += '</head>';
    documentContent += '<body>' + printContents + '</body></html>'
    popupWinindow.document.write(documentContent);
    popupWinindow.document.close();

    let tmr = setTimeout(function () {
      popupWinindow.print();
      popupWinindow.close();
    }, 200);

    this.AfterPrintAction();
  }
  Close() {
    this.showPatientSticker = false;
  }
  Error(err) {
    this._msgBoxServ.showMessage("error", ["Sorry!!! not able to get for patient- sticker"]);
    console.log(err.ErrorMessage);
  }
  AfterPrintAction() {
    if (this.closePopUpAfterStickerPrint) {
      this.afterPrintAction.emit({ showPatientSticker: false });
    }

  }

  //06April2018 print from server
  printStickerServer() {
    let printContents = document.getElementById("OPDsticker").innerHTML;
    var printableHTML = `<style>
        .opdstickercontainer {
        width: 370px;
        margin: 0px;
        display: block;
        font-size: 13px;
      }
  
      .stkrtopsection {
        width: 100%;
      }
  
     .dptdesc-left {
        width: 80%;
        display: inline-block;
        margin - top: 5px
      }
  
      .opd-qrcode {
        width: 15%;
        display: inline-block;
        vertical-align: top;
        float: right;
        margin: 8px 15px 0 0;
      }
      </style>`;
    printableHTML += '<meta charset="utf-8">';
    printableHTML += '<body>' + printContents + '</body></html>';
    var PrinterName = this.coreService.AllPrinterSettings.find(a => a.PrintingType === 'server' && a.GroupName === 'reg-sticker').PrinterDisplayName;;
    PrinterName += this.PatientStickerDetails.HospitalNo;
    var filePath = this.coreService.AllPrinterSettings.find(a => a.PrintingType === 'server' && a.GroupName === 'reg-sticker').ServerFolderPath;
    var lastCharacter = filePath.substr(filePath.length - 1);
    if (lastCharacter != '\\') {
      filePath += '\\';
    }
    this.loading = true;
    this.showLoading = true;
    this.http.post<any>("/api/Billing/saveHTMLfile?PrinterName=" + PrinterName + "&FilePath=" + filePath, printableHTML, this.options)
      .map(res => res).subscribe(res => {
        if (res.Status === ENUM_DanpheHTTPResponseText.OK) {
          this.timerFunction();
        }
        else {
          this.loading = false;
          this.showLoading = false;
        }
      });

    this.AfterPrintAction();
  }
  //loads Printer Setting from Paramter Table (database) -- ramavtar
  LoadPrinterSetting() {
    let Parameter = this.coreService.Parameters;
    Parameter = Parameter.filter(parms => parms.ParameterName === "DefaultPrinterName");
    let JSONobject = JSON.parse(Parameter[0].ParameterValue);
    let PrinterName = JSONobject.OPDSticker;
    return PrinterName;
  }
  //set button for preview
  showHidePrintButton() {
    let params = this.coreService.Parameters;
    params = params.filter(p => p.ParameterName === "showServerPrintBtn");
    let jsonObj = JSON.parse(params[0].ParameterValue);
    let value = jsonObj.OPDSticker;
    if (value === "true") {
      this.showServerPrintBtn = true;
    }
    else {
      this.showServerPrintBtn = false;
    }
  }

  //timer function
  timerFunction() {
    var timer = Observable.timer(10000);
    var sub: Subscription;
    sub = timer.subscribe(t => {
      this.showLoading = false;
    });
  }
  GetLocalDate(): string {
    var currParameter = this.coreService.Parameters.find(a => a.ParameterName === "CalendarTypes")
    if (currParameter) {
      let visitCalendar = JSON.parse(currParameter.ParameterValue).PatientVisit;
      if (visitCalendar === "en,np") {
        return this.nepaliCalendarServ.ConvertEngToNepDateString(moment().format(ENUM_DateTimeFormat.Year_Month_Day));
      }
    }
    else {
      this._msgBoxServ.showMessage("error", ["Please set local date view configuration."]);
      return null;
    }
  }
  //loads maximum follow up days limit from parameters
  loadMaximumFollowUpDays() {
    let maxLimit = this.coreService.Parameters.filter(p => p.ParameterGroupName === "Appointment" && p.ParameterName === "MaximumLastVisitDays");
    if (maxLimit[0]) {
      this.maxFollowUpDays = maxLimit[0].ParameterValue;
    }
  }


  public print() {
    this.coreService.loading = true;
    if (!this.selectedPrinter || this.selectedPrinter.PrintingType === ENUM_PrintingType.browser) {
      this.browserPrintContentObj = document.getElementById("patientprintpage");
      this.openBrowserPrintWindow = false;
      this.changeDetector.detectChanges();
      this.openBrowserPrintWindow = true;
      this.coreService.loading = false;
    } else if (this.selectedPrinter.PrintingType === ENUM_PrintingType.dotmatrix) {
      //-----qz-tray start----->
      this.coreService.QzTrayObject.websocket.connect()
        .then(() => {
          return this.coreService.QzTrayObject.printers.find();
        })
        .then(() => {
          var config = this.coreService.QzTrayObject.configs.create(this.selectedPrinter.PrinterName);
          let printOutType = "reg-sticker";
          let dataToPrint = this.PrintDotMatrix();
          return this.coreService.QzTrayObject.print(config, CommonFunctions.GetEpsonPrintDataForPage(dataToPrint, this.selectedPrinter.mh, this.selectedPrinter.ml, this.selectedPrinter.ModelName, printOutType));

        })
        .catch(function (e) {
          console.error(e);
        })
        .finally(() => {
          this.coreService.loading = false;
          return this.coreService.QzTrayObject.websocket.disconnect();
        });
      //-----qz-tray end----->
    } else if (this.selectedPrinter.PrintingType === ENUM_PrintingType.server) {

      this.printStickerServer();
      this.coreService.loading = false;
    }
    else {
      this._msgBoxServ.showMessage('error', ["Printer Not Supported."]);
      this.coreService.loading = true;
      return;
    }
  }


  public printDetaiils: any;


  public PrintDotMatrix() {
    let nline = '\n';
    let finalDataToPrint = "";
    finalDataToPrint += "Name:" + this.PatientStickerDetails.PatientName + " " + this.ageSex + nline;
    finalDataToPrint += "Hosp. No:" + this.PatientStickerDetails.HospitalNo + " " + (this.PatientStickerDetails.Contact ? "Contact :" + this.PatientStickerDetails.Contact : "") + nline;

    let address = '';
    // if (this.PatientStickerDetails.CountryName == 'Nepal') {
    //   address = "Address:" + (this.PatientStickerDetails.Address ? this.PatientStickerDetails.Address : "") + " " + this.PatientStickerDetails.District + nline;
    // } else {
    //   address = "Address:" + (this.PatientStickerDetails.Address ? this.PatientStickerDetails.Address : "") + " " + this.PatientStickerDetails.CountryName + nline;
    // }

    if (this.PatientStickerDetails.CountryName === ENUM_Country.Nepal) {
      address = "Address: " + ((this.showMunicipality && this.PatientStickerDetails.MunicipalityName) ? this.PatientStickerDetails.MunicipalityName + (this.PatientStickerDetails.WardNumber ? "-" + this.PatientStickerDetails.WardNumber : "") + ", " : "") + this.PatientStickerDetails.CountrySubDivisionName + nline;
    }
    else {
      address = "Address: " + (this.PatientStickerDetails.Address ? this.PatientStickerDetails.Address + ", " : "") + this.PatientStickerDetails.CountrySubDivisionName + ", " + this.PatientStickerDetails.CountryName + nline;
    }
    finalDataToPrint += address;
    finalDataToPrint += 'Type: ' + this.PatientStickerDetails.MembershipTypeName
      + ((this.PatientStickerDetails.SSFPolicyNo && (this.PatientStickerDetails.MembershipTypeName === ENUM_MembershipTypeName.SSF)) ? '  ' + 'SSF Policy No: ' + this.PatientStickerDetails.SSFPolicyNo : "")
      + ((this.PatientStickerDetails.PolicyNo && (this.PatientStickerDetails.MembershipTypeName === ENUM_MembershipTypeName.ECHS)) ? '  ' + 'ECHS No: ' + this.PatientStickerDetails.PolicyNo : "")
      + nline;
    finalDataToPrint += "Printed By: " + this.PrintedBy + " " + "Printed On: " + this.localDateTime + nline;
    return finalDataToPrint;
  }


  public SetPrinterFromParam() {
    this.printByDotMatrixPrinter = this.coreService.EnableDotMatrixPrintingInRegistration();
    if (this.printByDotMatrixPrinter) {
      if (!this.coreService.billingDotMatrixPrinters || !this.coreService.billingDotMatrixPrinters.length) {
        this.coreService.billingDotMatrixPrinters = this.coreService.GetBillingDotMatrixPrinterSettings();
      }

      this.billingDotMatrixPrinters = this.coreService.billingDotMatrixPrinters;

      if (!this.billingService.OpdStickerDotMatrixPageDimension) {
        this.billingService.OpdStickerDotMatrixPageDimension = this.coreService.GetDotMatrixPrinterDimensions(); //1 is for ins billing
      }

      this.dotPrinterDimensions = this.billingService.OpdStickerDotMatrixPageDimension;

      let prntrInStorage = localStorage.getItem('BillingStickerPrinter');
      if (prntrInStorage) {
        let val = this.billingDotMatrixPrinters.find(p => p.DisplayName === prntrInStorage);
        this.PrinterDisplayName = val ? val.DisplayName : '';
        this.PrinterNameDotMatix = val ? val.PrinterName : '';
      } else {
        this.showPrinterChange = true;
      }
    }
  }

  public ShowPrinterLocationChange() {
    let ptr = this.billingDotMatrixPrinters.find(p => p.DisplayName === this.PrinterDisplayName);
    this.PrinterNameDotMatix = ptr ? ptr.PrinterName : '';
    this.showPrinterChange = true;
  }

  public selectedPrinter: PrinterSettingsModel = new PrinterSettingsModel();
  OnPrinterChanged($event) {
    this.selectedPrinter = $event;
  }



  PrintSheet() {
    let documentContent = "<html><head>";
    documentContent += '</head>';
    documentContent += '<body onload="window.print()">' + this.PrintSheetTemplate + '</body></html>'
    const iframe = document.createElement('iframe');
    document.body.appendChild(iframe);
    iframe.contentWindow.document.open();
    iframe.contentWindow.document.write(documentContent);
    iframe.contentWindow.document.close();

    setTimeout(function () {
      document.body.removeChild(iframe);
    }, 500);
  }

  LoadPatientLatestVistDetails(patientId: number) {
    this.http.get<any>('/api/Stickers/GetPatientVisitDetails?PatientId=' + patientId, this.options)
      .map((res: DanpheHTTPResponse) => res)
      .subscribe(res => {
        if (res.Status === ENUM_DanpheHTTPResponseText.OK && res.Results) {
          if (res.Results) {
            this.PatientVisitDetails = res.Results;
            this.ChoosePrintSheetTemplate();
          }
        }
      },
        err => {
          this._msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, [err]);
        });
  }

  ChoosePrintSheetTemplate() {
    if (this.PatientVisitDetails && this.PatientVisitDetails.VisitType) {
      switch (this.PatientVisitDetails.VisitType) {
        case ENUM_VisitType.inpatient:
          this.SetPrintSheetTemplate(ENUM_PrintSheetTemplateVisitType.Inpatient, this.PatientVisitDetails.DepartmentCode);
          break;
        case ENUM_VisitType.outdoor:
        case ENUM_VisitType.outpatient:
          this.SetPrintSheetTemplate(ENUM_PrintSheetTemplateVisitType.Outpatient, this.PatientVisitDetails.DepartmentCode);
          break;
        case ENUM_VisitType.emergency:
          this.SetPrintSheetTemplate(ENUM_PrintSheetTemplateVisitType.Emergency, '');
          break;
      }
    }

  }
  SetPrintSheetTemplate(department: string, departmentCode: string) {
    //if department code is present then load department template else load default template
    //if department template is not present then load default template

    const defaultTemplateCode = (ENUM_PrintSheetTemplateCodes.TemplatePrefix + "_" + department + "_" + ENUM_PrintSheetTemplateCodes.DefaultTemplateSuffix);
    const departmentTemplateCode = (ENUM_PrintSheetTemplateCodes.TemplatePrefix + "_" + department + "_" + departmentCode);
    (async (): Promise<void> => {
      try {
        if (departmentCode) {
          await this.GetTemplateByTemplateCode(departmentTemplateCode, this.PatientVisitDetails.PatientVisitId);
          if (!this.PrintSheetTemplate) {
            await this.GetTemplateByTemplateCode(defaultTemplateCode, this.PatientVisitDetails.PatientVisitId);
          }
        }
        else
          await this.GetTemplateByTemplateCode(defaultTemplateCode, this.PatientVisitDetails.PatientVisitId);
      } catch (err) {
        this._msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, [`Error: ${err.ErrorMessage}`]);
      }
    })();
  }

  async GetTemplateByTemplateCode(templateCode: string, patientVisitId: number): Promise<void> {
    try {
      const res: DanpheHTTPResponse = await this.http.get<DanpheHTTPResponse>(`/api/NewClinical/Template?templateCode=${templateCode}&patientVisitId=${patientVisitId}`, this.options).toPromise();
      if (res.Status === ENUM_DanpheHTTPResponseText.OK && res.Results) {
        if (res.Results && res.Results.TemplateHTML)
          this.PrintSheetTemplate = res.Results.TemplateHTML;
        else
          this.PrintSheetTemplate = '';
      }
    }
    catch (err) {
      this._msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, [err]);
    }
  }
}

