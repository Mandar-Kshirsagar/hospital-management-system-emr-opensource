import { ChangeDetectorRef, Component, Input } from "@angular/core";
import * as moment from "moment";
import { CoreService } from "../../../core/shared/core.service";
import { SecurityService } from "../../../security/shared/security.service";
import { CommonFunctions } from "../../../shared/common.functions";
import { ENUM_BillDepositType, ENUM_PrintTemplateTypes, ENUM_ServiceCategoryCodes } from "../../../shared/shared-enums";
import { PrintTemplateType } from '../../shared/print-template-type.model';

@Component({
  selector: 'detailed-discharge-print',
  templateUrl: './detailed-discharge-print.component.html'
})
export class DetailedDischargePrintComponent {
  @Input("result-from-server")
  public ResultFromServerToBreak: any = null;

  @Input("IsDischarged")
  public IsDischarged: boolean = false;

  @Input("is-provisional-discharge")
  public IsProvisionalDischarge: boolean = false;

  @Input("estimated-DiscountAmount")
  public EstimatedDiscountAmount: number = 0;
  public headerDetail: { CustomerName, Address, Email, CustomerRegLabel, CustomerRegNo, Tel };
  public PatientDetail: any;
  public AdmissionInfo: any;
  public billItems = [];
  public InvoiceInfo: any;

  public AdmissionCharges = [];
  public BedCharges = [];
  public HospitalCharges = [];
  public DepositDetails = [];
  public ConsumableCharges = [];
  public InvestigationChargesServiceDepartmentWise = new Array<InvestigationChargesWithServiceDepartment>();
  public InvestigationCharges = [];
  public MedicineCharges = [];
  public ServiceCharges = [];
  public ProcedureCharges = [];
  public OperationCharges = [];
  public OtherCharges = [];

  public bedChargesTotal = new TotalCalculation();
  public depositTotals = { TotalDeposit: 0, DepositAdded: 0, DepositDeduct: 0 };
  public serviceChargesTotals = new TotalCalculation();
  public consumableChargesTotals = new TotalCalculation();
  public medicineChargesTotals = new TotalCalculation();
  public otherChargesTotal = new TotalCalculation();
  public procedureChargesTotal = new TotalCalculation();
  public operationChargesTotal = new TotalCalculation();
  public grandTotals = { SubTotal: 0, DiscountAmount: 0, TotalAmount: 0, Deposit: 0, ReceivedAmount: 0, CoPayCreditAmount: 0 };
  public currentUserName: string = "";
  public currDate: string = null;
  public currTime: string = null;
  public InvoiceDisplaySettings: any = { ShowHeader: true, ShowQR: true, ShowHospLogo: true, ShowPriceCategory: false, HeaderType: '' };
  public ShowProviderName: boolean = false;
  public OtherCurrencyDetail: OtherCurrencyDetail = { CurrencyCode: '', ExchangeRate: 0, BaseAmount: 0, ConvertedAmount: 0 };
  public ToBePaid: number = 0;
  public DepositUsed: number = 0;
  ToBeReturned: number = 0;
  public PrintTemplateTypeSettings = new PrintTemplateType();
  public loading: boolean = false;
  printContent: string = "";
  DisplayAsStatement: boolean = false;
  constructor(
    public coreService: CoreService,
    private _changeDetector: ChangeDetectorRef,
    private _securityService: SecurityService
  ) {
    this.GetHeaderDetails();
    this.InvoiceDisplaySettings = this.coreService.GetInvoiceDisplaySettings();
    this.ShowProviderName = this.coreService.SetShowProviderNameFlag();
    this.ReadDischargeStatementConfigParameter();
  }

  ReadDischargeStatementConfigParameter() {
    const param = this.coreService.Parameters.find(p => p.ParameterGroupName === "Billing" && p.ParameterName === "DisplayAsStatement");
    if (param) {
      const paramValue = JSON.parse(param.ParameterValue);
      this.DisplayAsStatement = paramValue;
    }
  }
  ngOnInit(): void {
    if (this.ResultFromServerToBreak) {
      this.ReadPrintReceiptDisplaySettingParameter();
      this.currTime = moment().format("HH:mm").toString();
      this.currDate = moment().format("YYYY-MM-DD").toString();
      this.currentUserName = this._securityService.loggedInUser.UserName;
      this.PatientDetail = this.ResultFromServerToBreak.PatientDetail;
      this.AdmissionInfo = this.ResultFromServerToBreak.AdmissionInfo;
      if (this.IsDischarged && !this.IsProvisionalDischarge) {
        this.InvoiceInfo = this.ResultFromServerToBreak.InvoiceInfo;
        if (this.InvoiceInfo && this.InvoiceInfo.OtherCurrencyDetail) {
          this.OtherCurrencyDetail = JSON.parse(this.InvoiceInfo.OtherCurrencyDetail);
        } else {
          this.OtherCurrencyDetail = null;
        }
      }
      this.GroupItems(this.ResultFromServerToBreak);
    }
  }
  ngAfterViewInit(): void {
    //Called after ngAfterContentInit when the component's view has been initialized. Applies to components only.
    //Add 'implements AfterViewInit' to the class.
    let htmlElement = document.getElementById("id_dynamic_discharge_detail");
    if (htmlElement) {
      let dischargeInvoiceData = document.createElement('div');
      dischargeInvoiceData.innerHTML = this.ResultFromServerToBreak.InvoicePrintTemplateSummary;
      this.printContent = this.ResultFromServerToBreak.InvoicePrintTemplateSummary;
      document.getElementById('id_dynamic_discharge_detail').appendChild(dischargeInvoiceData);
      this._changeDetector.detectChanges();
    }

  }

  ReadPrintReceiptDisplaySettingParameter() {
    let currParam = this.coreService.Parameters.find(a => a.ParameterGroupName === "Common" && a.ParameterName === "UseDynamicInvoicePrint");
    if (currParam && currParam.ParameterValue) {
      const paramValue = JSON.parse(currParam.ParameterValue) as Array<PrintTemplateType>;

      if (paramValue && this.IsDischarged && !this.IsProvisionalDischarge && this.ResultFromServerToBreak.InvoiceInfo && this.ResultFromServerToBreak.InvoiceInfo.PrintTemplateType) {
        const printTemplateSettings = paramValue.find(p => JSON.parse(this.ResultFromServerToBreak.InvoiceInfo.PrintTemplateType).includes(p.PrintType));
        if (printTemplateSettings && printTemplateSettings.PrintType === ENUM_PrintTemplateTypes.IpDischargeStatementDetailed) {
          this.PrintTemplateTypeSettings = paramValue.find(p => JSON.parse(this.ResultFromServerToBreak.InvoiceInfo.PrintTemplateType).includes(p.PrintType));
        } else {
          this.PrintTemplateTypeSettings.Enable = false;
        }
      }

      if (paramValue && !this.IsDischarged && this.ResultFromServerToBreak.PrintTemplateTypes) {
        this.PrintTemplateTypeSettings = paramValue.find(p => JSON.parse(this.ResultFromServerToBreak.PrintTemplateTypes).includes(p.PrintType));
      }
    }
  }
  GroupItems(resultFromServerToBreak): void {
    if (resultFromServerToBreak) {
      this.MedicineCharges = resultFromServerToBreak.PharmacyPendingBillsItems;
      this.DepositDetails = resultFromServerToBreak.DepositInfo;
      this.billItems = resultFromServerToBreak.BillItems;
      if (this.IsDischarged && !this.IsProvisionalDischarge) {
        this.DepositUsed = resultFromServerToBreak.InvoiceInfo.DepositUsed;
      }
      this.billItems.forEach((itm, index) => {
        if (!this.IsDischarged) {
          itm.TotalAmount = itm.SubTotal - itm.DiscountAmount;
        }
        if (itm.ServiceCategoryCode === ENUM_ServiceCategoryCodes.BedCharges) {
          this.BedCharges.push(itm);
        } else if (itm.ServiceCategoryCode === ENUM_ServiceCategoryCodes.InvestigationCharges) {
          this.InvestigationCharges.push(itm);
        } else if (itm.ServiceCategoryCode === ENUM_ServiceCategoryCodes.ServiceCharges) {
          this.ServiceCharges.push(itm);
        } else if (itm.ServiceCategoryCode === ENUM_ServiceCategoryCodes.ConsumableCharges) {
          this.ConsumableCharges.push(itm);
        } else if (itm.ServiceCategoryCode === ENUM_ServiceCategoryCodes.ProcedureCharges) {
          this.ProcedureCharges.push(itm);
        } else if (itm.ServiceCategoryCode === ENUM_ServiceCategoryCodes.OperationCharges) {
          this.OperationCharges.push(itm);
        } else {
          this.OtherCharges.push(itm);
        }
      });

      if (this.InvestigationCharges && this.InvestigationCharges.length > 0) {
        this.InvestigationChargesServiceDepartmentWise = this.InvestigationCharges.reduce((acc, item) => {
          //!Krishna, 4thJuly'23, Below logic is written bases on reference logic, Here acc is referred to existingServiceDep Variable,
          //!so that when pushed to existingServiceDep it is automatically reflected to acc.
          const existingServiceDep = acc.find(servDep => servDep.ServiceDepartmentId === item.ServiceDepartmentId);

          if (existingServiceDep) {
            existingServiceDep.SubTotal += item.SubTotal;
            existingServiceDep.DiscountAmount += item.DiscountAmount;
            existingServiceDep.TotalAmount += item.TotalAmount;

            existingServiceDep.SubTotal = CommonFunctions.parseAmount(existingServiceDep.SubTotal, 2);
            existingServiceDep.DiscountAmount = CommonFunctions.parseAmount(existingServiceDep.DiscountAmount, 2);
            existingServiceDep.TotalAmount = CommonFunctions.parseAmount(existingServiceDep.TotalAmount, 2);

            existingServiceDep.InvestigationCharges.push(item);
          } else {
            const investigationChargesWithServiceDep = new InvestigationChargesWithServiceDepartment();
            investigationChargesWithServiceDep.ServiceDepartmentId = item.ServiceDepartmentId;
            investigationChargesWithServiceDep.ServiceDepartmentName = item.ServiceDepartmentName;
            investigationChargesWithServiceDep.SubTotal += item.SubTotal;
            investigationChargesWithServiceDep.DiscountAmount += item.DiscountAmount;
            investigationChargesWithServiceDep.TotalAmount += item.TotalAmount;
            investigationChargesWithServiceDep.InvestigationCharges.push(item);

            investigationChargesWithServiceDep.SubTotal = CommonFunctions.parseAmount(investigationChargesWithServiceDep.SubTotal, 2);
            investigationChargesWithServiceDep.DiscountAmount = CommonFunctions.parseAmount(investigationChargesWithServiceDep.DiscountAmount, 2);
            investigationChargesWithServiceDep.TotalAmount = CommonFunctions.parseAmount(investigationChargesWithServiceDep.TotalAmount, 2);
            acc.push(investigationChargesWithServiceDep);
          }
          return acc;
        }, []);


      }

      if (this.BedCharges && this.BedCharges.length > 0) {
        this.CalculateBedChargesTotal();
      }
      if (this.DepositDetails && this.DepositDetails.length > 0) {
        this.CalculateDepositDetailsTotal();
      }
      if (this.ConsumableCharges && this.ConsumableCharges.length > 0) {
        this.CalculateConsumablesChargesTotal();
      }
      if (this.ServiceCharges && this.ServiceCharges.length > 0) {
        this.CalculateServiceChargesTotal();
      }
      if (this.MedicineCharges && this.MedicineCharges.length > 0) {
        this.CalculateMedicineChargesTotal();
      }
      if (this.OtherCharges && this.OtherCharges.length > 0) {
        this.CalculateAmbulanceChargesTotal();
      }
      if (this.ProcedureCharges && this.ProcedureCharges.length > 0) {
        this.CalculateProcedureChargesTotal();
      }
      if (this.OperationCharges && this.OperationCharges.length > 0) {
        this.CalculateOperationChargesTotal();
      }

      this.CalculateGrandTotals();
    }
  }

  CalculateBedChargesTotal() {
    this.BedCharges.forEach(ch => {
      this.bedChargesTotal.SubTotal += ch.SubTotal;
      this.bedChargesTotal.DiscountAmount += ch.DiscountAmount;
      this.bedChargesTotal.TotalAmount += ch.TotalAmount;
    });
    this.bedChargesTotal.SubTotal = CommonFunctions.parseAmount(this.bedChargesTotal.SubTotal, 2);
    this.bedChargesTotal.DiscountAmount = CommonFunctions.parseAmount(this.bedChargesTotal.DiscountAmount, 2);
    this.bedChargesTotal.TotalAmount = CommonFunctions.parseAmount(this.bedChargesTotal.TotalAmount, 2);
  }

  CalculateServiceChargesTotal() {
    this.ServiceCharges.forEach(ch => {
      this.serviceChargesTotals.SubTotal += ch.SubTotal;
      this.serviceChargesTotals.DiscountAmount += ch.DiscountAmount;
      this.serviceChargesTotals.TotalAmount += ch.TotalAmount;
    });
    this.serviceChargesTotals.SubTotal = CommonFunctions.parseAmount(this.serviceChargesTotals.SubTotal, 2);
    this.serviceChargesTotals.DiscountAmount = CommonFunctions.parseAmount(this.serviceChargesTotals.DiscountAmount, 2);
    this.serviceChargesTotals.TotalAmount = CommonFunctions.parseAmount(this.serviceChargesTotals.TotalAmount, 2);
  }

  CalculateConsumablesChargesTotal() {
    this.ConsumableCharges.forEach(ch => {
      this.consumableChargesTotals.SubTotal += ch.SubTotal;
      this.consumableChargesTotals.DiscountAmount += ch.DiscountAmount;
      this.consumableChargesTotals.TotalAmount += ch.TotalAmount;
    });
    this.consumableChargesTotals.SubTotal = CommonFunctions.parseAmount(this.consumableChargesTotals.SubTotal, 2);
    this.consumableChargesTotals.DiscountAmount = CommonFunctions.parseAmount(this.consumableChargesTotals.DiscountAmount, 2);
    this.consumableChargesTotals.TotalAmount = CommonFunctions.parseAmount(this.consumableChargesTotals.TotalAmount, 2);
  }

  CalculateMedicineChargesTotal() {
    this.MedicineCharges.forEach(med => {
      this.medicineChargesTotals.SubTotal += med.SubTotal;
      this.medicineChargesTotals.DiscountAmount += med.DiscountAmount;
      this.medicineChargesTotals.TotalAmount += med.TotalAmount;
      this.medicineChargesTotals.SubTotal = CommonFunctions.parseAmount(this.medicineChargesTotals.SubTotal, 2);
      this.medicineChargesTotals.DiscountAmount = CommonFunctions.parseAmount(this.medicineChargesTotals.DiscountAmount, 2);
      this.medicineChargesTotals.TotalAmount = CommonFunctions.parseAmount(this.medicineChargesTotals.TotalAmount, 2);
    });
  }

  CalculateDepositDetailsTotal() {
    this.DepositDetails.forEach(dep => {
      if (dep.TransactionType !== ENUM_BillDepositType.Deposit) {
        this.depositTotals.TotalDeposit -= dep.OutAmount;
        this.depositTotals.DepositDeduct += dep.OutAmount;
      } else {
        this.depositTotals.TotalDeposit += dep.InAmount;
        this.depositTotals.DepositAdded += dep.InAmount;
      }
    });
  }
  CalculateAmbulanceChargesTotal() {
    this.OtherCharges.forEach(amb => {
      this.otherChargesTotal.SubTotal += amb.SubTotal;
      this.otherChargesTotal.DiscountAmount += amb.DiscountAmount;
      this.otherChargesTotal.TotalAmount += amb.TotalAmount;


      this.otherChargesTotal.SubTotal = CommonFunctions.parseAmount(this.otherChargesTotal.SubTotal, 2);
      this.otherChargesTotal.DiscountAmount = CommonFunctions.parseAmount(this.otherChargesTotal.DiscountAmount, 2);
      this.otherChargesTotal.TotalAmount = CommonFunctions.parseAmount(this.otherChargesTotal.TotalAmount, 2);

    });
  }
  CalculateProcedureChargesTotal() {
    this.ProcedureCharges.forEach(amb => {
      this.procedureChargesTotal.SubTotal += amb.SubTotal;
      this.procedureChargesTotal.DiscountAmount += amb.DiscountAmount;
      this.procedureChargesTotal.TotalAmount += amb.TotalAmount;


      this.procedureChargesTotal.SubTotal = CommonFunctions.parseAmount(this.procedureChargesTotal.SubTotal, 2);
      this.procedureChargesTotal.DiscountAmount = CommonFunctions.parseAmount(this.procedureChargesTotal.DiscountAmount, 2);
      this.procedureChargesTotal.TotalAmount = CommonFunctions.parseAmount(this.procedureChargesTotal.TotalAmount, 2);

    });
  }

  CalculateOperationChargesTotal() {
    this.OperationCharges.forEach(amb => {
      this.operationChargesTotal.SubTotal += amb.SubTotal;
      this.operationChargesTotal.DiscountAmount += amb.DiscountAmount;
      this.operationChargesTotal.TotalAmount += amb.TotalAmount;

      this.operationChargesTotal.SubTotal = CommonFunctions.parseAmount(this.operationChargesTotal.SubTotal, 2);
      this.operationChargesTotal.DiscountAmount = CommonFunctions.parseAmount(this.operationChargesTotal.DiscountAmount, 2);
      this.operationChargesTotal.TotalAmount = CommonFunctions.parseAmount(this.operationChargesTotal.TotalAmount, 2);

    });
  }

  CalculateGrandTotals() {
    const billingSubTotal = this.billItems.reduce((acc, item) => acc + item.SubTotal, 0);
    let billingDiscount = 0;
    if (this.IsDischarged) {
      billingDiscount = this.billItems.reduce((acc, item) => acc + item.DiscountAmount, 0);
    } else {
      billingDiscount = this.EstimatedDiscountAmount > 0 ? this.EstimatedDiscountAmount : 0;
    }
    const billingTotal = this.billItems.reduce((acc, item) => acc + item.TotalAmount, 0);

    const pharmacySubTotal = this.MedicineCharges.reduce((acc, item) => acc + item.SubTotal, 0);
    const pharmacyDiscount = this.MedicineCharges.reduce((acc, item) => acc + item.DiscountAmount, 0);
    const pharmacyTotal = this.MedicineCharges.reduce((acc, item) => acc + item.TotalAmount, 0);

    this.grandTotals.SubTotal = billingSubTotal + pharmacySubTotal;
    this.grandTotals.DiscountAmount = billingDiscount + pharmacyDiscount;
    if (this.IsDischarged) {
      this.grandTotals.TotalAmount = billingTotal + pharmacyTotal;
    } else {
      this.grandTotals.TotalAmount = this.grandTotals.SubTotal - this.grandTotals.DiscountAmount;
    }

    if (!this.IsDischarged || this.IsProvisionalDischarge) {
      if (this.depositTotals.TotalDeposit > 0 && this.depositTotals.TotalDeposit >= this.grandTotals.TotalAmount) {
        this.DepositUsed = this.grandTotals.TotalAmount;
      }

      if (this.depositTotals.TotalDeposit > 0 && this.depositTotals.TotalDeposit < this.grandTotals.TotalAmount) {
        this.DepositUsed = this.depositTotals.TotalDeposit;
      }
    }

    if ((this.DepositUsed > 0) && (this.grandTotals.TotalAmount >= this.DepositUsed)) {
      this.ToBePaid = this.grandTotals.TotalAmount - this.DepositUsed;
    }
    else {
      this.ToBePaid = this.grandTotals.TotalAmount;
    }

    if (this.depositTotals.TotalDeposit > 0) {
      if (this.DepositUsed === this.grandTotals.TotalAmount) {
        this.ToBeReturned = this.depositTotals.TotalDeposit - this.DepositUsed;
      } else {
        this.ToBeReturned = 0;
      }
    } else {
      this.ToBeReturned = 0;
    }

    if (this.IsDischarged && this.DepositUsed === this.grandTotals.TotalAmount) {
      this.ToBeReturned = this.InvoiceInfo.DepositReturnAmount;
    }
  }

  GetHeaderDetails(): void {
    let paramValue = this.coreService.Parameters.find(a => a.ParameterName === 'BillingHeader').ParameterValue;
    if (paramValue) {
      this.headerDetail = JSON.parse(paramValue);
    }
  }
  CalculateCalculateAgeAndAddGender(dateOfBirth): string {
    if (dateOfBirth) {
      let dobYear: number = Number(moment(dateOfBirth).format("YYYY"));
      // if (dobYear > 1920) {
      let ageString = String(Number(moment().format("YYYY")) - Number(moment(dateOfBirth).format("YYYY")));
      return `${ageString} Y/${this.PatientDetail.Gender}`;
      // }
    }
  }

  public openBrowserPrintWindow: boolean = false;
  public browserPrintContentObj: any;
  public print() {
    this.browserPrintContentObj = document.getElementById("id_detailed_discharge_print");
    this.openBrowserPrintWindow = false;
    this._changeDetector.detectChanges();
    this.openBrowserPrintWindow = true;
    // this._changeDetector.detectChanges();
  }
  public printReceipt() {
    this.loading = true;
    //Open 'Browser Print' if printer not found or selected printing type is Browser.
    // this.browserPrintContentObj = this.printContent;
    // this.openBrowserPrintWindow = false;
    // this._changeDetector.detectChanges();
    // this.openBrowserPrintWindow = true;
    this.GenerateDynamicInvoicePrintBrowser(this.printContent);
    this.loading = false;

  }
  GenerateDynamicInvoicePrintBrowser(dataToPrint: string) {
    let iframe = document.createElement('iframe');
    document.body.appendChild(iframe);
    iframe.contentWindow.document.open();
    iframe.contentWindow.document.write(dataToPrint);
    iframe.contentWindow.document.close();

    setTimeout(function () {
      document.body.removeChild(iframe);
    }, 500);

  }
}

export class InvestigationChargesWithServiceDepartment {
  public ServiceDepartmentId: number = null;
  public ServiceDepartmentName: string = "";
  public InvestigationCharges = [];
  public SubTotal: number = 0;
  public DiscountAmount: number = 0;
  public TotalAmount: number = 0;
}

export class TotalCalculation {
  public SubTotal: number = 0;
  public DiscountAmount: number = 0;
  public TotalAmount: number = 0;
}
