import { Component, EventEmitter, Input, Output } from "@angular/core";
import { CoreService } from "../../../core/shared/core.service";
import { DispensaryService } from "../../../dispensary/shared/dispensary.service";
import { PrinterSettingsModel } from "../../../settings-new/printers/printer-settings.model";
import { DanpheHTTPResponse } from "../../../shared/common-models";
import { GeneralFieldLabels } from "../../../shared/DTOs/general-field-label.dto";
import { MessageboxService } from "../../../shared/messagebox/messagebox.service";
import { ENUM_DanpheHTTPResponseText, ENUM_MessageBox_Status } from "../../../shared/shared-enums";
import { PharmacyBLService } from "../../shared/pharmacy.bl.service";
import { PharmacyPatientConsumptionReturnInfo_DTO } from "../shared/phrm-patient-consumption-return-info.dto.";
@Component({
  selector: 'phrm-return-consumption-receipt',
  templateUrl: './consumption-return-receipt.component.html'
})

export class ConsumptionReturnReceiptComponent {

  public receipt: PharmacyPatientConsumptionReturnInfo_DTO = new PharmacyPatientConsumptionReturnInfo_DTO();
  @Input("patient-consumption-return-receipt-no") public PatientConsumptionReturnReceiptNo: number = 0;
  IsItemLevelVATApplicable: boolean = false;
  IsMainVATApplicable: boolean = false;
  IsItemLevelDiscountApplicable: boolean = false;
  IsMainDiscountAvailable: boolean = false;
  showFooter: boolean;
  showEnglish: boolean;
  englishText: string = '';
  showNepali: boolean = false;
  nepaliText: string = '';
  showGenericName: boolean = false;
  showItemName: boolean = false;
  showGenNameAfterItemName: false;
  LeadingSeparator: string = '';
  public headerDetail: { hospitalName, address, email, PANno, tel, DDA };
  public selectedPrinter: PrinterSettingsModel = new PrinterSettingsModel();
  public openBrowserPrintWindow: boolean = false;
  public browserPrintContentObj: any = { innerHTML: '' };

  public GeneralFieldLabel = new GeneralFieldLabels();
  @Output("call-back-print") callBackPrint: EventEmitter<object> = new EventEmitter();
  showRackNoInPrint: boolean = false;


  constructor(public coreService: CoreService,
    public pharmacyBLService: PharmacyBLService,
    public messageBoxService: MessageboxService,
    public _dispensaryService: DispensaryService
  ) {
    this.GeneralFieldLabel = coreService.GetFieldLabelParameter();
    this.GetRackNoParameterSettings();

  }

  ngOnInit() {
    this.GetPharmacyInvoiceFooterParameter();
    this.GetPharmacyBillingHeaderParameter();
    this.GetPharmacyItemNameDisplaySettings();

    if (this.PatientConsumptionReturnReceiptNo) {
      this.GetPatientConsumptionReturnInfo(this.PatientConsumptionReturnReceiptNo);
    }
  }

  GetPharmacyInvoiceFooterParameter() {
    let InvFooterParameterStr = this.coreService.Parameters.find(p => p.ParameterName == "PharmacyInvoiceFooterNoteSettings" && p.ParameterGroupName == "Pharmacy");
    if (InvFooterParameterStr != null) {
      let FooterParameter = JSON.parse(InvFooterParameterStr.ParameterValue);
      if (FooterParameter.ShowFooter == true) {
        this.showFooter = true;
        if (FooterParameter.ShowEnglish == true) {
          this.showEnglish = true;
          this.englishText = FooterParameter.EnglishText;
        }
        if (FooterParameter.ShowNepali == true) {
          this.showNepali = true;
          this.nepaliText = FooterParameter.NepaliText;
        }
      }
    }
  }

  GetPharmacyBillingHeaderParameter() {
    var paramValue = this.coreService.Parameters.find(a => a.ParameterName == 'Pharmacy BillingHeader').ParameterValue;
    if (paramValue)
      this.headerDetail = JSON.parse(paramValue);
    else
      this.messageBoxService.showMessage(ENUM_MessageBox_Status.Error, ["Please enter parameter values for BillingHeader"]);
  }

  GetPharmacyItemNameDisplaySettings() {
    let checkGeneric = this.coreService.Parameters.find(p => p.ParameterName == "PharmacyItemNameDisplaySettings" && p.ParameterGroupName == "Pharmacy");
    if (checkGeneric != null) {
      let phrmItemNameSettingValue = JSON.parse(checkGeneric.ParameterValue);
      this.showGenericName = phrmItemNameSettingValue.Show_GenericName
      this.showItemName = phrmItemNameSettingValue.Show_ItemName;
      this.showGenNameAfterItemName = phrmItemNameSettingValue.Show_GenericName_After_ItemName;
      this.LeadingSeparator = phrmItemNameSettingValue.Separator.trim();
    }
  }


  public GetPatientConsumptionReturnInfo(ConsumptionReturnReceiptNo: number) {
    this.pharmacyBLService.GetPatientConsumptionReturnInfo(ConsumptionReturnReceiptNo).subscribe((res: DanpheHTTPResponse) => {
      if (res.Status === ENUM_DanpheHTTPResponseText.OK) {
        this.receipt = res.Results;
        this.UpdateItemDisplayName(this.showGenericName, this.showItemName, this.LeadingSeparator, this.showGenNameAfterItemName);
      }
      else {
        this.messageBoxService.showMessage(ENUM_MessageBox_Status.Failed, ['Failed to get return info.']);
      }
    },
      err => {
        this.messageBoxService.showMessage(ENUM_MessageBox_Status.Failed, ['Failed to get return info. See console for more info.']);
        console.log(err);
      });
  }

  /**
  * Display the ItemName in the receipts based on core cfg parameter "PharmacyItemNameDisplaySettings"
  * @param showGenericName true if generic name should be seen
  * @param showItemName true if item name should be seen
  * @param separator string that separates itemname and genericname, wild card value "brackets" uses '()' to separate item name and generic name
  * @param showGenericNameAfterItemName true if itemname should be seen first and genericname should be seen after
  * @returns void
  * @default Shows only ItemName
  * @description created by Rohit on 4th Oct, 2021
  */
  public UpdateItemDisplayName(showGenericName: boolean, showItemName: boolean, separator: string = '', showGenericNameAfterItemName: boolean) {
    for (var i = 0; i < this.receipt.PatientConsumptionReturnItems.length; i++) {
      var item = this.receipt.PatientConsumptionReturnItems[i];
      if (showGenericName == true && showItemName == false) {
        item.ItemDisplayName = item.GenericName;
      }
      else if (showGenericName == false && showItemName == true) {
        item.ItemDisplayName = item.ItemName;
      }
      else if (showGenericName == true && showItemName == true) {
        if (showGenericNameAfterItemName == true) {
          if (separator == "" || separator.toLowerCase() == "brackets") {
            item.ItemDisplayName = `${item.ItemName}(${item.GenericName})`;
          }
          else {
            item.ItemDisplayName = item.ItemName + separator + item.GenericName;
          }
        }
        else {
          if (separator == "" || separator.toLowerCase() == "brackets") {
            item.ItemDisplayName = `${item.GenericName}(${item.ItemName})`;
          }
          else {
            item.ItemDisplayName = item.GenericName + separator + item.ItemName;
          }
        }
      }
      else {
        item.ItemDisplayName = item.ItemName;
      }
    }
  }

  OnPrinterChanged($event) {
    this.selectedPrinter = $event;
  }

  public Print(idToBePrinted: string = 'dv_patient-consumption-print') {
    this.browserPrintContentObj.innerHTML = document.getElementById(idToBePrinted).innerHTML;
    this.openBrowserPrintWindow = true;
  }

  CallBackBillPrint($event) {

  }
  GetRackNoParameterSettings(): void {
    let RackNoPrintSetting = this.coreService.Parameters.find(p => p.ParameterName == "ShowRackNoInPharmacyReceipt" && p.ParameterGroupName == "Pharmacy");
    if (RackNoPrintSetting) {
      let showRackNoInPrint = JSON.parse(RackNoPrintSetting.ParameterValue);
      this.showRackNoInPrint = showRackNoInPrint.ShowRackNoInNormalPharmacyReceipt;
    }
  }

}