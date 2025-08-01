import { HttpClient } from "@angular/common/http";
import { ChangeDetectorRef, Component, EventEmitter, Input, OnInit, Output, Renderer2, ViewChild } from "@angular/core";
import { Lightbox } from "angular2-lightbox";
import * as _ from 'lodash';
import * as moment from "moment";
import { CoreService } from "../../core/shared/core.service";
import { SecurityService } from "../../security/shared/security.service";
import { GeneralFieldLabels } from "../DTOs/general-field-label.dto";
import { DLService } from "../dl.service";
import { InvoiceHeaderModel } from "../invoice-header.model";
import { MessageboxService } from "../messagebox/messagebox.service";
import { ENUM_DanpheHTTPResponseText, ENUM_MessageBox_Status } from "../shared-enums";
@Component({
  selector: 'add-invoice-header',
  templateUrl: './add-invoice-header.html'
})

export class AddInvoiceHeaderComponent implements OnInit {

  @Input("allInvoiceHeaderList")
  public AllInvoiceHeaderList = new Array<InvoiceHeaderModel>();
  @ViewChild("fileInput") fileInput;

  public showAddPage: boolean = false;
  @Input('selectedInvoiceHeader')
  public eidtableHeader: InvoiceHeaderModel;

  public imgURL: any = null;
  public selectedInvoiceHeader: InvoiceHeaderModel = new InvoiceHeaderModel();
  public editMode: boolean = false;
  public module: string = "";
  public loading: boolean;
  @Output('call-back')
  public callbackAdd: EventEmitter<Object> = new EventEmitter<Object>();
  public isFileValid: boolean = true;
  public globalListenFunc: Function;
  public ESCAPE_KEYCODE = 27;//to close the window on click of ESCape.
  public GeneralFieldLabel = new GeneralFieldLabels();

  @Input('Module')
  public set moduleValue(value) {
    this.module = value;

  }

  constructor(private _http: HttpClient,
    public securityService: SecurityService,
    public msgBoxSrv: MessageboxService,
    public dlService: DLService,
    public lightbox: Lightbox,

    public changeDetector: ChangeDetectorRef, public renderer2: Renderer2, public coreservice: CoreService,) {
    this.GeneralFieldLabel = coreservice.GetFieldLabelParameter();

  }

  @Input('showAddPage')
  public set value(val) {
    this.showAddPage = val;
    if (this.eidtableHeader && this.eidtableHeader.InvoiceHeaderId > 0) {
      this.editMode = true;
      this.selectedInvoiceHeader = Object.assign(this.selectedInvoiceHeader, this.eidtableHeader);
      this.selectedInvoiceHeader.Module = this.module;
      if (this.selectedInvoiceHeader.FileBinaryData) {
        this.imgURL = 'data:image/jpeg;base64,' + this.selectedInvoiceHeader.FileBinaryData;
      }
    }
    else {
      this.editMode = false;
      this.selectedInvoiceHeader = new InvoiceHeaderModel();
      this.selectedInvoiceHeader.Module = this.module;
      this.changeDetector.detectChanges();
      this.setFocusById('hospital');
    }
  }

  ngOnInit() {
    this.globalListenFunc = this.renderer2.listen('document', 'keydown', e => {
      if (e.keyCode == this.ESCAPE_KEYCODE) {
        this.Close()
      }
    });
  }
  SubmitHeader() {
    try {
      this.loading = true;
      this.selectedInvoiceHeader.Module = this.module;
      let file = null;
      file = this.fileInput.nativeElement.files[0];

      //Header details Validation
      var isValid = true;
      if (this.selectedInvoiceHeader) {
        var isValid = this.CheckValidation();
      }

      // Logo Image file Validation
      if (file) {
        this.isFileValid = this.ValidateImageSize(file)
      }
      else if (!file && this.editMode) { // Not showing file error in edit mode, file may not be changed. 
        this.isFileValid = true;
        file = null;
      } else {
        this.isFileValid = false;
        this.loading = false;
        this.msgBoxSrv.showMessage("error", ["No File Selected!"]);
      }

      if (isValid && this.isFileValid) {
        this.selectedInvoiceHeader.CreatedOn = moment().format("YYYY-MM-DD");
        var headerDetails: any = this.selectedInvoiceHeader;

        // Drafting Data for post (Logo Image file detais and Header details)
        let formToPost = new FormData();
        if (headerDetails) {
          var fileName: string = "";
          if (file) {
            let splitImagetype = file.name.split(".");
            let fileExtension = splitImagetype[1];
            fileName = headerDetails.Module + "_InvoiceLogo_" + moment() + "." + fileExtension;
            headerDetails.LogoFileName = fileName;
            headerDetails.LogoFileExtention = fileExtension;
            formToPost.append("uploads", file, fileName);
          }
          var tempFD: any = _.omit(headerDetails, ['HeaderValidators']);
          var tempHeaderDetails = JSON.stringify(tempFD);
          formToPost.append("fileDetails", tempHeaderDetails);
        }

        if (!this.editMode) {
          this.PostInvoiceHeader(formToPost);

        } else if (this.editMode) {
          this.PutInvoiceHeader(formToPost);
        }
        else {
          this.msgBoxSrv.showMessage("error", ["Error"]);
          this.loading = false;
        }
      }
    } catch (ex) {
      console.log(ex);
      //this.msgBoxSrv.showMessage("error", [ex]);
    }
  }

  public ValidateImageSize(file) {
    let flag = true;
    if (file.size < 10485000) {
      flag = true;
    } else {
      this.msgBoxSrv.showMessage("error", ["Size of file must be less than 10 MB !"]);
      flag = false;
    }
    return flag;
  }

  PostInvoiceHeader(formToPost): void {

    if (this.AllInvoiceHeaderList && this.AllInvoiceHeaderList.length) {

      // Assuming formToPost is a FormData object, extracting data from it
      let hospitalName = '';

      if (formToPost instanceof FormData) {
        const fileDetails = formToPost.get("fileDetails");
        if (fileDetails) {
          const details = JSON.parse(fileDetails as string);
          hospitalName = details.HospitalName ? details.HospitalName.toLowerCase() : '';
        }
      } else {
        hospitalName = formToPost.HospitalName ? formToPost.HospitalName.toLowerCase() : '';
      }

      // Checking for duplicates
      const isHospitalNameAlreadyExists = this.AllInvoiceHeaderList.some(a =>
        a.HospitalName && a.HospitalName.toLowerCase() === hospitalName
      );

      if (isHospitalNameAlreadyExists) {
        this.msgBoxSrv.showMessage(ENUM_MessageBox_Status.Notice, [`Cannot Add Invoice Header as the Header Name "${hospitalName}" already exists.`]);
        this.loading = false;
        return;
      }

    }

    try {
      /*Bikash: 12July'20 :
         * this component can be used billing, inventory and pharmacy and
         * there is no service that is shared by these modules,
         * hence, the api call has been made directly here.
        */
      this._http.post<any>("/api/PharmacySettings/InvoiceHeader", formToPost)
        .subscribe(
          res => {
            if (res.Status === ENUM_DanpheHTTPResponseText.OK) {
              this.Close();
              this.msgBoxSrv.showMessage(ENUM_MessageBox_Status.Success, ['Image is Uploded']);
              this.SendCallBack(res);
            }
            else if (res.Status === ENUM_DanpheHTTPResponseText.Failed) {
              this.msgBoxSrv.showMessage(ENUM_MessageBox_Status.Error, ['Failed']);
              this.loading = false;
            }
            else {
              this.loading = false;
              this.msgBoxSrv.showMessage(ENUM_MessageBox_Status.Failed, [res.ErrorMessage]);
            }
          },
          err => {
            console.log(err);
          });

    } catch (exception) {
      // this.ShowCatchErrMessage(exception);
    }

  }

  PutInvoiceHeader(formToPost): void {
    if (this.AllInvoiceHeaderList && this.AllInvoiceHeaderList.length) {

      // Extracting data from formToPost
      let hospitalName = '';
      let currentId = 0;

      if (formToPost instanceof FormData) {
        const fileDetails = formToPost.get("fileDetails");
        if (fileDetails) {
          const details = JSON.parse(fileDetails as string);
          hospitalName = details.HospitalName ? details.HospitalName.toLowerCase() : '';
          currentId = details.InvoiceHeaderId;
        }
      } else {
        hospitalName = formToPost.HospitalName ? formToPost.HospitalName.toLowerCase() : '';
        currentId = formToPost.InvoiceHeaderId;
      }

      // Check for duplicates excluding the current record
      const isHospitalNameAlreadyExists = this.AllInvoiceHeaderList.some(a =>
        a.HospitalName && a.HospitalName.toLowerCase() === hospitalName && a.InvoiceHeaderId !== currentId
      );

      if (isHospitalNameAlreadyExists) {
        this.msgBoxSrv.showMessage(ENUM_MessageBox_Status.Notice, [`Cannot Update Invoice Header as the Header Name "${hospitalName}" already exists.`]);
        this.loading = false;
        return;
      }
    }
    try {
      /*Bikash: 12July'20 :
       * this component can be used billing, inventory and pharmacy and
       * there is no service that is shared by these modules,
       * hence, the api call has been made directly here.
      */
      this._http.put<any>("/api/PharmacySettings/InvoiceHeader", formToPost)
        .subscribe(
          res => {
            if (res.Status == "OK") {
              this.Close();
              this.msgBoxSrv.showMessage("success", ['Image is Uploded']);
              this.SendCallBack(res);
            }
            else if (res.Status == "Failed") {
              this.msgBoxSrv.showMessage("error", ['Failed']);
              this.loading = false;
            }
            else
              this.msgBoxSrv.showMessage("failed", [res.ErrorMessage]);
          },
          err => {
            console.log(err);
          });

    } catch (exception) {
      // this.ShowCatchErrMessage(exception);
    }
  }
  public CheckValidation(): boolean {
    var isValid: boolean = true;

    for (var i in this.selectedInvoiceHeader.HeaderValidators.controls) {
      this.selectedInvoiceHeader.HeaderValidators.controls[i].markAsDirty();
      this.selectedInvoiceHeader.HeaderValidators.controls[i].updateValueAndValidity();
    }
    if (this.selectedInvoiceHeader.IsValidCheck(undefined, undefined)) {
      isValid = true
    }
    else {
      isValid = false;
      this.loading = false;
      this.msgBoxSrv.showMessage("Notice-Message", ["Please insert all required details.."]);
    }
    return isValid;
  }

  public ShowLogoPreview(files) {
    this.imgURL = null;
    if (files.length > 0) {
      var mimeType = files[0].type;
      if (mimeType.match(/image\/*/) == null) {
        this.msgBoxSrv.showMessage("error", ["Only images are supported."]);
        return;
      }

      var reader = new FileReader();
      reader.readAsDataURL(files[0]);
      reader.onload = (_event) => {
        this.imgURL = reader.result;
      }
      this.isFileValid = true;
    } else {
      var reader = new FileReader();
    }
  }
  Close() {
    this.selectedInvoiceHeader = new InvoiceHeaderModel();
    this.loading = false;
    this.editMode = false;
    this.imgURL = null;
    this.showAddPage = false;
  }
  SendCallBack(res) {
    if (res.Status == "OK") {
      this.callbackAdd.emit({ invoiceHeader: res.Results });
    }
    else {
      this.msgBoxSrv.showMessage("error", ['Check log for details']);
    }
  }
  setFocusById(IdToBeFocused) {
    window.setTimeout(function () {
      document.getElementById(IdToBeFocused).focus();
    }, 20);
  }

}
