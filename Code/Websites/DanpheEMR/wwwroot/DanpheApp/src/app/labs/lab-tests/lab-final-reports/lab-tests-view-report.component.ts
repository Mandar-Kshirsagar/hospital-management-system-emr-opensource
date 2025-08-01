/*
 Description:
   - It is a reusable component and is used in lab-tests-results.component.ts
   - templateReport is passed from lab-tests-results.component.ts
   - It uses lab-signatories.component.ts
   - It displays the lab-report.
   - It either updates/posts LabReport.
    
 -------------------------------------------------------------------
 change history:
 -------------------------------------------------------------------
 S.No     UpdatedBy/Date             description           remarks
 -------------------------------------------------------------------
 1.       ashim/ 3rd July 2018           created            
                                                     
 -------------------------------------------------------------------
 */

import {
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  Output
} from "@angular/core";
import { Router } from "@angular/router";
import * as moment from "moment/moment";
import { MessageboxService } from "../../../shared/messagebox/messagebox.service";
import { LabReportVM } from "../../reports/lab-report-vm";
import { LabReport } from "../../shared/lab-report";
import { LabsBLService } from "../../shared/labs.bl.service";
//import * as htmlDocx from 'htmlDocx';
import { DomSanitizer } from "@angular/platform-browser";
import html2canvas from "html2canvas";
import * as jsPDF from "jspdf";
import * as _ from "lodash";
import { Employee } from "../../../../../src/app/employee/shared/employee.model";
import { SecurityService } from "../../../../../src/app/security/shared/security.service";
import { CoreService } from "../../../core/shared/core.service";
import { NepaliCalendarService } from "../../../shared/calendar/np/nepali-calendar.service";
import {
  DanpheCache,
  MasterType,
} from "../../../shared/danphe-cache-service-utility/cache-services";
import { ENUM_DanpheHTTPResponseText, ENUM_MessageBox_Status } from "../../../shared/shared-enums";
import { LabTestComponent } from "../../shared/lab-component.model";
import {
  LabResult_TemplatesVM,
  LabResult_TestVM,
} from "../../shared/lab-view.models";
import { LabService } from "../../shared/lab.service";

@Component({
  selector: "danphe-lab-view-report",
  templateUrl: "./lab-tests-view-report.html",
  styleUrls: ["./lab-tests-view-report.style.css"],
})
export class LabTestsViewReportComponent {
  @Input("showReport")
  public showReport: boolean;
  @Input("showHeader")
  public showHeader: boolean = true;
  @Input("showSignatories")
  public showSignatories: boolean = true;
  public templateReport: LabReportVM = null;
  public signatories: string;
  public showSignatoriesEdit: boolean = true;
  public loading: boolean = false;
  public showPopUp: boolean = false;
  @Input("enableEdit")
  public enableEdit: boolean = true;
  @Input("showCustomHeader")
  public showCustomHeader: boolean = false;

  @Input('create-report-for-claim')
  public CreateReportForClaim: boolean = false;

  @Output("callbackBackToGrid") callbackAddUpdate: EventEmitter<object> =
    new EventEmitter<object>();
  @Output("callBackToReportDispatch")
  callbackToReportDispatch: EventEmitter<object> = new EventEmitter<object>();

  @Output("callBackToReportAddUpdate")
  callBackToReportAddUpdate: EventEmitter<object> = new EventEmitter<object>();

  @Output("callBackUplaod") callBackUplaod: EventEmitter<any> = new EventEmitter<any>();
  @Output("updatePrintStatus") updatePrintStatus: EventEmitter<any> = new EventEmitter<any>();

  @Input("hidePrintButton")
  public hidePrintButton: boolean = false;

  @Input("showUplaodToTeleMedicine")
  public showUplaodToTeleMedicine: boolean = false;

  @Input("IsFileUploaded")
  public IsFileUploaded: boolean = false;

  public IsTeleMedicineEnabled: boolean = false;
  public doctorsList: Array<any> = [];
  public requisitionIdList: Array<number> = new Array<number>();
  public defaultSigEmpIdList: Array<number>;
  public oldName: string = null;
  public showChangeSample: boolean = false;
  public showConfirmationBox: boolean = false;
  public sampleCode = { RunNumber: 0, SampleCreatedOn: null, SampleCode: 0 };
  public sampleCodeExistingDetail = {
    Exisit: false,
    PatientName: null,
    PatientId: null,
    SampleCreatedON: null,
  };
  public visitType = null;
  public RunNumberType = null;
  public CurrentDateTime: string = null;
  public CreatedByUser: Employee = null;
  public showLoggedInUserSignatory: boolean = false;
  public showReportDispatcherSignatory: boolean = false;

  public verificationEnabled: boolean = false;
  public PreVerificationEnabled: boolean = false;
  @Input() public verificationRequired: boolean = false;

  public hospitalCode: string = "";

  public isCultureRptLoaded: boolean = false; //sud:3Sept'18
  public showIntermediateInCultureRpt: boolean = false; //bring it from parameters later on.
  public cultureRptSpecimen: string = "Urine"; //default specimen for Culture

  public hasInsurance: boolean = false;
  public showBarCode: boolean = false;
  public showHideHighLowNormalFlag: boolean = false;

  @Input("printReportFromGrid")
  public printDirectlyFromGrid: boolean = true;

  public showPrintInfo: boolean = false;
  public showDigitalSignature: boolean = false;

  public showVerifierSignature: boolean = false;
  public preliminaryText: string = null;
  public preliminarySignature: string = null;
  public verifierSignatureList: Array<any> = [];

  public allEmployeeList: Array<Employee> = new Array<Employee>();

  //@ViewChild("exportContent") content: ElementRef;

  public allowLabNoChange: boolean = true;
  public allowReferredByChange: boolean = true;
  public routeAfterVerification: string;
  public qrCode: string;
  public allValues: any;
  public collectionSite: string;
  public prescriberLabelInLabReport: string = 'Prescriber Name';
  public ProfilePicSRC: string = "";
  public isCovidTest: boolean = false;
  public CovidTestName: string = "";
  public IsSignatureValid: boolean = false;
  public LabReportDisplayParameter = { "ShowChangeReferredBy": false, "ShowChangeLabNumber": false, "ShowCollectionSite": true, "ShowSpecimen": false, "ShowWard": false, "ShowVerificationDate": false, "ShowPassportNo": false, "ShowProfilePic": false, "ShowChangePrescriberName": true, "ShowPolicyNumber": false };
  @Input("showReportFromClaimScrubbing")
  showReportFromClaimScrubbing: boolean = false;
  @Input("ShowPrintButtonFromClaimScrubbing")
  showPrintButtonFromClaimScrubbing = false;
  public LabSignatoryParameter = {
    "EnableSignatoriesEdit": false
  };
  constructor(
    public labBLService: LabsBLService,
    public labService: LabService,
    public npCalendarService: NepaliCalendarService,
    public messageBoxService: MessageboxService,
    public changeDetector: ChangeDetectorRef,
    public sanitizer: DomSanitizer,
    public coreService: CoreService,
    public router: Router,
    public securityService: SecurityService
  ) {
    this.GetLabReportDisplayParam();
    this.CurrentDateTime = moment().format("YYYY-MM-DD HH:mm");
    this.GetDoctorsList();
    this.CreatedByUser = this.securityService.GetLoggedInUser().Employee;

    let TeleMedicineConfig = this.coreService.Parameters.find(p => p.ParameterGroupName == "TeleMedicine" && p.ParameterName == "DanpheConfigurationForTeleMedicine").ParameterValue;
    this.IsTeleMedicineEnabled = JSON.parse(JSON.parse(TeleMedicineConfig).IsTeleMedicineEnabled);
    this.allValues = this.coreService.GetAllParametersDataForLabReport();
    if (this.allValues) {
      this.prescriberLabelInLabReport = this.allValues.prescriberLabelInLabReport;
      this.showLoggedInUserSignatory = Boolean(JSON.parse(this.allValues.LoggedInUserSignatory));
      this.showReportDispatcherSignatory = Boolean(JSON.parse(this.allValues.ReportDispatcherSignature));
      this.showPrintInfo = Boolean(JSON.parse(this.allValues.DisplayPrintInfo));
      this.showBarCode = Boolean(JSON.parse(this.allValues.LabBarCodeInReport));
      if (!_.isEmpty(this.allValues.LabReportVerificationB4Print)) {
        this.verificationEnabled = Boolean(JSON.parse(this.allValues.LabReportVerificationB4Print.EnableVerificationStep));
        this.PreVerificationEnabled = Boolean(JSON.parse(this.allValues.LabReportVerificationB4Print.EnablePreVerification));
        this.preliminaryText = this.allValues.LabReportVerificationB4Print.PreliminaryReportText;
        this.preliminarySignature = this.allValues.LabReportVerificationB4Print.PreliminaryReportSignature;
        this.showVerifierSignature = Boolean(JSON.parse(this.allValues.LabReportVerificationB4Print.ShowVerifierSignature));
      }
      this.hospitalCode = this.allValues.HospitalCode;
      this.showIntermediateInCultureRpt = Boolean(JSON.parse(this.allValues.CultureIntermediateResults));
      this.showHideHighLowNormalFlag = Boolean(JSON.parse(this.allValues.HighLowNormalFlag));
      this.showDigitalSignature = Boolean(JSON.parse(this.allValues.DigitalSignatureEnabled));
      this.collectionSite = this.allValues.CollectionSite;

      let covidTestParam = this.coreService.Parameters.find(a => a.ParameterGroupName == 'Common' && a.ParameterName == 'CovidTestName');
      if (covidTestParam) {
        this.CovidTestName = JSON.parse(covidTestParam.ParameterValue).DisplayName;
      }
    }

    if (!this.hospitalCode) {
      this.hospitalCode = "default-lab-report";
    }

    if (!this.hospitalCode) {
      this.hospitalCode = "default-lab-report";
    }

    var coreParam = this.coreService.Parameters.find(
      (a) =>
        a.ParameterGroupName.toLowerCase() == "lab" &&
        a.ParameterName == "LabReportDisplaySettings"
    );
    var paramValue = JSON.parse(coreParam.ParameterValue);
    this.allowLabNoChange = paramValue.ShowChangeLabNumber;
    this.allowReferredByChange = paramValue.ShowChangeReferredBy;
    if (this.labService.routeNameAfterverification) {
      if (
        this.labService.routeNameAfterverification.toLowerCase() == "addresult"
      ) {
        this.routeAfterVerification = "PendingLabResults";
      } else if (
        this.labService.routeNameAfterverification.toLowerCase() ==
        "finalreports"
      ) {
        this.routeAfterVerification = "FinalReports";
      } else if (
        this.labService.routeNameAfterverification.toLowerCase() ==
        "pendingreports"
      ) {
        this.routeAfterVerification = "PendingReports";
      }
    }
  }

  ngOnInit() {
    //if (this.verificationRequired) {
    //  this.reportBg = true;
    //} else { this.reportBg = false; }
  }

  ngAfterViewInit() {
    this.changeDetector.detectChanges();
    let btnObj = document.getElementById("btnUpdateSignatories");
    if (btnObj) {
      btnObj.focus();
    }
  }

  ngAfterViewChecked() {
    var doc = document.getElementById("lab-report-main");
    if (this.printDirectlyFromGrid && doc) {
      this.loading = true;
      this.printDirectlyFromGrid = false;
      this.printLabReport();
    }
  }

  @Input("templateReport")
  public set tempReport(_templateReport: LabReportVM) {
    this.allEmployeeList = DanpheCache.GetData(MasterType.Employee, null);
    this.isCultureRptLoaded = true; //reset value at beginning: sud: 3sept'18

    if (_templateReport) {
      this.ProfilePicSRC = _templateReport.Lookups.ProfilePictureName;
      _templateReport.Templates.forEach(a => a.Tests
        .forEach(a => {
          if (a.TestName == this.CovidTestName) {
            this.isCovidTest = true;
          }
        }));
      this.qrCode = _templateReport.CovidFileUrl;
      this.hasInsurance = _templateReport.HasInsurance;
      _templateReport.VerifiedByList = [];
      if (_templateReport.Lookups.VisitType.toLowerCase() == "outpatient") {
        _templateReport.Lookups.VisitTypeCode = "OP";
      } else if (
        _templateReport.Lookups.VisitType.toLowerCase() == "inpatient"
      ) {
        _templateReport.Lookups.VisitTypeCode = "IP";
      } else {
        _templateReport.Lookups.VisitTypeCode = "ER";
      }

      _templateReport["PrintFooterText"] = false;

      if (_templateReport.Templates.length) {
        _templateReport.Templates.forEach((temp) => {
          if (
            !_templateReport.FooterText ||
            _templateReport.FooterText.trim() == ""
          ) {
            if (temp.FooterText && temp.FooterText.trim() != "") {
              _templateReport.FooterText = temp.FooterText;
              _templateReport["PrintFooterText"] = true;
            }
          } else {
            _templateReport["PrintFooterText"] = true;
          }

          temp["Print"] = true;
          if (temp.Tests.length) {
            temp.Tests.forEach((test) => {
              if (
                _templateReport.VerifiedByList &&
                !_templateReport.VerifiedByList.includes(test.VerifiedBy)
              ) {
                _templateReport.VerifiedByList.push(test.VerifiedBy);
              }
              test["Print"] = true;
              test["PrintInterpretation"] = true;
              this.requisitionIdList.push(test.RequisitionId);
              if (test.Components.length) {
                test.Components.forEach((component) => {
                  component["Print"] = true;
                });
              }
            });
          }
        });
      }

      if (_templateReport.TemplateType == "html") {
        //IMPORTANT: sanitizer.bypassSecurity : Needed to retain style/css of innerHTML !! --sud:12Apr'18'
        _templateReport.Templates.forEach((template) => {
          template.Tests.forEach((result) => {
            result.Components.forEach((res) => {
              res.ValueHtml = this.sanitizer.bypassSecurityTrustHtml(res.Value);
            });
          });
        });
      } else {
        if (_templateReport.Templates.length) {
          _templateReport.Templates.forEach((temp) => {
            temp.ColCount = 0;
            if (this.showHideHighLowNormalFlag) {
              temp.ColCount = temp.ColCount + 1;
            }
            for (var col in temp.TemplateColumns) {
              if (temp.TemplateColumns[col]) {
                temp.ColCount = temp.ColCount + 1;
              }
              if (!temp.TemplateColumns.Method) {
                temp.ColCount = temp.ColCount + 1;
              }
            }

            if (
              temp.TemplateType &&
              temp.TemplateType.toLowerCase() == "culture"
            ) {
              if (temp.Tests != null && temp.Tests.length > 0) {
                temp.Tests.forEach((tst) => {
                  tst.ResultFormattedForCulture = [];
                  for (let grpInd = 1; grpInd <= tst.MaxResultGroup; grpInd++) {
                    tst.ResultFormattedForCulture.push(
                      LabReportVM.FormatResultForCulture(tst, grpInd)
                    );
                  }
                });
              }
            }
          });
        }
        this.isCultureRptLoaded = true;
      }

      this.templateReport = _templateReport;
      if (!this.templateReport.Signatories) {
        if (this.templateReport.TemplateType == "html") {
          this.defaultSigEmpIdList =
            this.coreService.GetDefaultHistoCytoEmpIdForLabSignatories();
        } else {
          this.defaultSigEmpIdList =
            this.coreService.GetDefaultEmpIdForLabSignatories();
          //let id = 1;
        }
      }
      this.oldName = this.templateReport.Lookups.PrescriberName;
      this.ParseSignatories(this.templateReport.Signatories);
    }
  }

  public ParseSignatories(_signatories) {
    if (_signatories) {
      var type = typeof _signatories;
      let signs = _signatories;
      if (type == "object") {
        if (this.showReportDispatcherSignatory) {
          _signatories.forEach((item, index) => {
            if (item.EmployeeId == this.CreatedByUser.EmployeeId) {
              this.showReportDispatcherSignatory = false;
            }
          });
        }
        if (this.showVerifierSignature) {
          signs.forEach((item, index) => {
            if (
              this.templateReport.VerifiedByList &&
              this.templateReport.VerifiedByList.includes(item.EmployeeId)
            ) {
              signs.splice(index, 1);
            }
          });
        }
        _signatories = JSON.stringify(_signatories);
        this.signatories = _signatories;
      } else {
        if (_signatories)
          signs = JSON.parse(_signatories);
        if (this.showReportDispatcherSignatory) {
          signs.forEach((item, index) => {
            if (item.EmployeeId == this.CreatedByUser.EmployeeId) {
              this.showReportDispatcherSignatory = false;
            }
          });
        }

        if (this.CreateReportForClaim) {
          signs.forEach(itm => {
            itm.Show = true;
          });
        }

        signs = JSON.stringify(signs);
        this.signatories = signs;
      }

      this.showSignatoriesEdit = false;
      this.verifierSignatureList = [];
      let signatory = [];
      if (this.signatories) {
        signatory = JSON.parse(this.signatories);
      }
      this.templateReport.VerifiedByList.forEach((ver) => {
        var empl = this.allEmployeeList.find((e) => e.EmployeeId == ver);
        if (empl) {
          var singleEmp = {
            EmployeeId: empl.EmployeeId,
            Signature: empl.LabSignature,
            DisplaySequence: empl.DisplaySequence,
            Show: false,
            SignatoryImageName: empl.SignatoryImageName,
          };
          let dup = signatory.find(a => a.EmployeeId == singleEmp.EmployeeId);
          if (!dup)
            this.verifierSignatureList.push(singleEmp);
        }
      });

      let signString = this.signatories;
      signString = signString
        .replace(/\n/g, "\\n")
        .replace(/\r/g, "\\r")
        .replace(/\t/g, "\\t");

      var signArr = JSON.parse(signString);

      signArr.forEach((sgn) => {
        if (
          !sgn.hasOwnProperty("SignatoryImageName") ||
          !sgn["SignatoryImageName"]
        ) {
          let currentEmp = this.allEmployeeList.find(
            (v) => v.EmployeeId == sgn.EmployeeId
          );
          if (currentEmp) {
            sgn["SignatoryImageName"] = currentEmp.SignatoryImageName;
            if (sgn.hasOwnProperty("Show") && sgn["Show"]) {
              sgn["Show"] = true;
            } else {
              sgn["Show"] = false;
            }
          }
        }
      });


      if (this.templateReport.ReportId) {
        if (this.verificationRequired) {
          this.coreService.FocusInputById('btnVerify');
        } else {
          if (this.templateReport.ValidToPrint) {
            this.coreService.FocusInputById('btnPrint');
          }
        }
      }

      signArr.sort((a, b) => {
        if (a.DisplaySequence > b.DisplaySequence) return 1;
        if (a.DisplaySequence < b.DisplaySequence) return -1;
        return 0;
      });

      this.signatories = JSON.stringify(signArr);
      this.templateReport.Signatories = signArr;
    } else {
      this.showSignatoriesEdit = true;
      this.coreService.FocusInputById('btnUpdateSignatories');
    }
  }

  public GetDoctorsList() {
    this.labBLService.GetDoctorsList().subscribe(
      (res) => {
        if (res.Status == "OK") {
          if (res.Results.length) {
            this.doctorsList = res.Results;
          } else {
            console.log(res.ErrorMessage);
          }
        }
      },
      (err) => {
        this.messageBoxService.showMessage("Failed", [
          "unable to get Doctors list.. check log for more details.",
        ]);
        console.log(err.ErrorMessage);
      }
    );
  }

  AssignedToDocListFormatter(data: any): string {
    return data["FullName"];
  }

  CheckIfAbnormal(comp: LabTestComponent) {
    //no need to check if the value itself is invalid

    if (comp && comp.RangeDescription) {
      comp.Range = comp.RangeDescription;

      comp.IsAbnormal = true;
      this.changeDetector.detectChanges();
      comp.IsAbnormal = false;

      //comp.Range && comp.ValueType  => True only if these fields are present & has some value.
      //check abnormal only for valuetype=number, for string we cannot detect which value is abnormal.--sud:11Apr'18

      if (comp.Range) {
        let value = Number(comp.Value.replace(/,/g, ""));
        if (comp.Range.includes("-")) {
          comp.Range = comp.Range.replace(/,/g, "");
          let range = comp.Range.split("-");
          if (value < Number(range[0]) || value > Number(range[1])) {
            comp.IsAbnormal = false;
            this.changeDetector.detectChanges();
            comp.IsAbnormal = true;
          }
        } else if (comp.Range.includes("<")) {
          let range = comp.Range.split("<");
          if (value > Number(range[1])) {
            comp.IsAbnormal = false;
            this.changeDetector.detectChanges();
            comp.IsAbnormal = true;
          }
        } else if (comp.Range.includes(">")) {
          let range = comp.Range.split(">");
          if (value < Number(range[1])) {
            comp.IsAbnormal = false;
            this.changeDetector.detectChanges();
            comp.IsAbnormal = true;
          }
        }
      }
    }
  }

  PostLabReport(exportType: string = null) {
    if (this.CheckSignatoriesValidation()) {
      //var createNew: boolean;
      //createNew = window.confirm('You wont be able to make further changes. Do you want to continue?');
      //if (!createNew)
      //    return;
      let labReport: LabReport = new LabReport();
      let reportDates = [];
      this.templateReport.Templates.forEach((template) => {
        template.Tests.forEach((test) => {
          test.Components.forEach((comp) => {
            labReport.ComponentIdList.push(comp.TestComponentResultId);
            reportDates.push(new Date(comp.CreatedOn));
          });
        });
      });
      let rcvDates = [];
      rcvDates.push(new Date(this.templateReport.Lookups.SampleDate));
      //let rcvDates = this.templateReport.Results.map(a => new Date());

      this.templateReport.Lookups.ReceivingDate = labReport.ReceivingDate =
        moment(new Date(Math.min.apply(null, rcvDates))).format(
          "YYYY-MM-DD HH:mm"
        );
      //ashim: 06Sep2018 : We're now using DateTime.Now for ReportingDate. It is handled in server side.
      //this.templateReport.Lookups.ReportingDate = labReport.ReportingDate = moment(new Date(Math.max.apply(null, reportDates))).format('YYYY-MM-DD HH:mm');

      //Post LabSignatories with default User As well
      if (this.showLoggedInUserSignatory || !this.LabSignatoryParameter.EnableSignatoriesEdit) {
        var currUser = { EmployeeId: 0, Signature: "", SignatoryImageName: "", Show: false, DisplaySequence: 1000 };
        currUser.EmployeeId = this.CreatedByUser.EmployeeId;
        currUser.Signature = this.CreatedByUser.LabSignature;
        currUser.SignatoryImageName = this.CreatedByUser.SignatoryImageName;
        currUser.DisplaySequence = this.CreatedByUser.DisplaySequence;
        let signObj = [];
        if (this.signatories)
          signObj = JSON.parse(this.signatories);
        if (
          this.CreatedByUser.LabSignature &&
          this.CreatedByUser.LabSignature.trim() != ""
        ) {
          var dupSign = signObj.find(
            (itm) => itm.EmployeeId == currUser.EmployeeId
          );
          //if currentLoggedInUser is not in the List then add It
          if (!dupSign) {
            signObj.push(currUser);
          }
        }

        this.signatories = JSON.stringify(signObj);
      }

      labReport.Signatories = this.signatories;
      //Post LabSignatories with default User Signature ends

      labReport.PatientId = this.templateReport.Lookups.PatientId;
      labReport.PrescriberName = this.templateReport.Lookups.PrescriberName;
      labReport.Comments = this.templateReport.Comments;
      labReport.VerificationEnabled = this.verificationEnabled;

      //ashim: 01Sep2018 : We're now grouping by only sample code.
      //labReport.TemplateId = this.templateReport.TemplateId;

      this.labBLService.PostLabReport(labReport).subscribe((res) => {
        if (res.Status == "OK") {
          this.templateReport.ReportId = res.Results.LabReportId;
          this.templateReport.ValidToPrint = res.Results.ValidToPrint;
          this.qrCode = this.templateReport.CovidFileUrl =
            res.Results.CovidFileUrl;
          this.ParseSignatories(labReport.Signatories);

          if (this.verificationEnabled) {
            this.router.navigate(["/Lab/PendingReports"]);
          }

          this.callBackToReportAddUpdate.emit({ added: true });

          if (exportType == "print") this.printLabReport();
          else if (exportType == "word") this.exportToWord();
          else if (exportType == "pdf") this.exportToPdf();
          this.loading = false;
        } else {
          this.messageBoxService.showMessage("failed", ["Unable to post Report."]);
          console.log(res.ErrorMessage);
          this.loading = false;
        }
      });
    }
  }

  UpdateLabReport() {
    if (this.CheckSignatoriesValidation()) {
      let labReport: LabReport = new LabReport();
      labReport.LabReportId = this.templateReport.ReportId;
      labReport.Comments = this.templateReport.Comments;

      //Update LabSignatories with default User As well
      if (this.showLoggedInUserSignatory || !this.LabSignatoryParameter.EnableSignatoriesEdit) {
        var currUser = { EmployeeId: 0, Signature: "", SignatoryImageName: "", Show: false, DisplaySequence: 1000 };
        currUser.EmployeeId = this.CreatedByUser.EmployeeId;
        currUser.Signature = this.CreatedByUser.LabSignature;
        currUser.SignatoryImageName = this.CreatedByUser.SignatoryImageName;
        currUser.DisplaySequence = this.CreatedByUser.DisplaySequence;
        let signObj = [];

        if (this.signatories)
          signObj = JSON.parse(this.signatories);
        var dupSign = signObj.find(
          (itm) => itm.EmployeeId == currUser.EmployeeId
        );
        //if currentLoggedInUser is not in the List then add It
        if (!dupSign) {
          signObj.push(currUser);
        }
        this.signatories = JSON.stringify(signObj);
      }

      labReport.Signatories = this.signatories;

      //Update LabSignatories with default User As well

      this.labBLService.PutLabReport(labReport).subscribe((res) => {
        if (res.Status == "OK") {
          this.ParseSignatories(labReport.Signatories);
          this.callBackToReportAddUpdate.emit({ added: true });
          this.loading = false;
        } else {
          this.messageBoxService.showMessage("failed", ["Unable to post Report."]);
          console.log(res.ErrorMessage);
          this.loading = false;
        }
      });
    }
  }
  CheckSignatoriesValidation() {
    //signatories component binds "[]" if empty so length >2 is checked.
    if (this.signatories && this.signatories.length > 2 || !this.LabSignatoryParameter.EnableSignatoriesEdit) {
      return true;
    } else {
      this.messageBoxService.showMessage("failed", ["Select Lab Signatories."]);
      return false;
    }
  }
  SubmitLabReport() {
    let sign = [];
    try {
      if (this.signatories)
        sign = JSON.parse(this.signatories);
    }
    catch {
      this.messageBoxService.showMessage(ENUM_MessageBox_Status.Failed, [`Please select valid signature`]);
      return;
    }
    this.loading = true;
    if (this.loading) {
      if (this.templateReport.ReportId) {
        this.UpdateLabReport();
      } else {
        this.PostLabReport();
        this.coreService.FocusInputById('btnVerify');
      }
    }
  }

  public Export(exportType: string) {
    if (this.templateReport.ReportId) {
      if (exportType == "print") this.printLabReport();
      if (exportType == "word") this.exportToWord();
      if (exportType == "pdf") this.exportToPdf();
    } else {
      this.PostLabReport(exportType);
    }
  }

  printLabReport() {
    this.CurrentDateTime = moment().format("YYYY-MM-DD HH:mm");

    if (
      this.templateReport.ReportId &&
      this.loading &&
      !this.printDirectlyFromGrid
    ) {
      var reqIdList = [];

      this.templateReport.Templates.forEach((tmp) => {
        if (tmp["Print"]) {
          tmp.Tests.forEach((test) => {
            if (test["Print"]) {
              reqIdList.push(test.RequisitionId);
            }
          });
        }
      });

      //var signatoriesToUpdate = JSON.stringify(this.templateReport.Signatories);

      this.labBLService
        .UpdateIsPrintedFlag(this.templateReport.ReportId, reqIdList)
        .subscribe((res) => {
          if (res.Status == "OK") {
            this.templateReport.IsPrinted = true;
            this.templateReport.PrintedOn = res.Results.PrintedOn;
            this.templateReport.PrintedBy = res.Results.PrintedBy;
            let emp = this.allEmployeeList.find(a => a.EmployeeId === res.Results.PrintedBy);
            this.templateReport.PrintedByName = emp ? emp.FullName : "";
            this.templateReport.PrintCount = res.Results.PrintCount;
            this.updatePrintStatus.emit(reqIdList);
            this.print(reqIdList);
          } else {
            this.messageBoxService.showMessage("failed", [
              "Error In Updating Print Informations in Report Table",
            ]);
            this.loading = false;
          }
        });
    } else {
      this.messageBoxService.showMessage("error", ["Cannot print the report."]);
      this.loading = false;
    }

    //if (!this.templateReport.IsPrinted) {

    //}
    //else {
    //  var copyStyle = '<style>div#lab-report-main:after{ position: absolute;top: 0px;right: 7px;display: block;content: "";font-size: 16px;font-style: italic;bottom: 0;}</style>';
    //  documentContent += copyStyle;
    //}
  }

  public print(printedRequisitionIdList: Array<number>) {
    let popupWinindow;
    if (document.getElementById("lab-report-main")) {
      document.getElementById("lab-report-main").style.border = "none";
    }
    if (document.getElementById("lab-report")) {
      var printContents = document.getElementById("lab-report").innerHTML;
    }

    popupWinindow = window.open(
      "",
      "_blank",
      "width=600,height=700,scrollbars=no,menubar=no,toolbar=no,location=no,status=no,titlebar=no"
    );
    popupWinindow.document.open();
    var documentContent = "<html><head>";
    documentContent +=
      `<link href="../../../../../../assets-dph/external/global/plugins/font-awesome/css/font-awesome.min.css" rel="stylesheet" type="text/css" />` +
      `<link rel="stylesheet" type="text/css" href="../../../../../../themes/theme-default/DanpheStyle.css" />` +
      `<link rel="stylesheet" type="text/css" href="../../../../../../themes/theme-default/DanphePrintStyle.css" /></head>`;

    documentContent +=
      '<body class="lab-rpt4moz">' + printContents + "</body></html>";
    popupWinindow.document.write(documentContent);
    document.getElementById("lab-report-main").style.border = "1px solid";
    popupWinindow.document.close();

    popupWinindow.onload = function () {
      popupWinindow.print();
      popupWinindow.close();
    }

    // let tmr = setTimeout(function () {
    //   popupWinindow.print();
    //   popupWinindow.close();
    // }, 300);

    if (this.printDirectlyFromGrid) {
      this.printDirectlyFromGrid = false;
      this.callbackAddUpdate.emit({
        printed: true,
        requisitionList: printedRequisitionIdList,
      });
    }

    this.callbackToReportDispatch.emit({
      printed: true,
      requisitionList: printedRequisitionIdList,
    });
    this.loading = false;
    this.printDirectlyFromGrid = false;
  }

  public getBase64Image(img) {
    //from here I've set the height and weight of the image. but somehow the image comes bigger.
    var canvas = document.createElement("canvas");
    canvas.width = img.clientWidth;
    canvas.height = img.clientHeight;
    var ctx = canvas.getContext("2d");
    ctx.drawImage(img, 0, 0, img.clientWidth, img.clientHeight);
    return canvas.toDataURL("image/jpg");
  }

  public exportToWord() {
    //this.showimage = true;
    let img = document.getElementById("headerImg");

    var originalHeight = document.getElementById("headerImg").clientHeight;
    var originalSource = document
      .getElementById("headerImg")
      .getAttribute("src");
    document.getElementById("lab-report-main").style.minHeight = 379 + "mm";
    document.getElementById("lab-report-main").style.border = "none";

    img.style.width = 6.4 + "in";
    img.style.height = "auto";
    if (img) {
      let baseURL = this.getBase64Image(img);
      document.getElementById("headerImg").setAttribute("src", baseURL);
    }
    let htmlStr = document.getElementById("lab-report").innerHTML;

    //let converted = htmlDocx.asBlob(htmlStr, { orientation: 'potrait', margins: { top: 720 } });

    let a = document.createElement("a");
    //a.href = URL.createObjectURL(converted);
    //ashim: 01Sep2018 : We're now grouping by only sample code.
    a.download =
      this.templateReport.Lookups.PatientName +
      "-" +
      moment().format("YYMMDDHHmm");
    // a.download = this.templateReport.Lookups.PatientName + "-" + this.templateReport.TemplateName + "-" + moment().format("YYMMDDHHmm");
    document.body.appendChild(a);
    a.click();
    img.style.width = 100 + "%";
    img.style.height = originalHeight + "px";
    document.getElementById("headerImg").setAttribute("src", originalSource);
    document.getElementById("lab-report-main").style.minHeight = 175 + "mm";
    document.getElementById("lab-report-main").style.border = "1px solid";
    document.getElementById("lab-report-main").style.border = "1px solid";
  }

  public exportToPdf() {
    //this.showimage = false;
    //const elementToPrint = document.getElementById('row'); //The html element to become a pdf
    //const pdf = new jsPDF('p', 'pt', 'a4');
    //pdf.addHTML(elementToPrint, () => {
    //    pdf.save('web.pdf');
    //});

    document.getElementById("lab-report-main").style.border = "none";
    document.getElementById("lab-report-main").style.minHeight = 250 + "mm";
    //document.getElementById("signatureList").s

    var doc = new jsPDF("p", "mm", "letter");
    //ashim: 01Sep2018 : We're now grouping by only sample code.
    var docName =
      this.templateReport.Lookups.PatientName +
      "-" +
      moment().format("YYMMDDHHmm") +
      ".pdf";
    //var docName = this.templateReport.Lookups.PatientName + "-" + this.templateReport.TemplateName + "-" + moment().format("YYMMDDHHmm") +'.pdf';

    html2canvas(document.getElementById("lab-report-main"), {
      width: 1200,
      windowWidth: 1200,
      scale: 1,
    }).then((canvas) => {
      var image = canvas.toDataURL("image/png");
      doc.addImage(image, "JPEG", 11, 15);
      doc.save(docName);
      document.getElementById("lab-report-main").style.border = "1px solid";
      document.getElementById("lab-report-main").style.minHeight = 175 + "mm";
    });
  }

  openPopUpBox() {
    this.showPopUp = false;
    this.changeDetector.detectChanges();
    this.showPopUp = true;
  }

  closePopUpBox() {
    this.showPopUp = false;
  }

  //AssignSelectedDoctor() {
  //  if (this.doctorSelected && this.doctorSelected.EmployeeId && !(this.templateReport.Lookups.ReferredById == this.doctorSelected.EmployeeId)) {
  //    this.UpdateDoctor();
  //  }

  //  this.showPopUp = false;
  //}

  AssignSelectedDoctor() {
    if (
      this.selectedPrescriberId &&
      this.selectedPrescriberName &&
      !(this.templateReport.Lookups.PrescriberId == this.selectedPrescriberId)
    ) {
      this.UpdateDoctor();
    }

    this.showPopUp = false;
  }

  //UpdateDoctor() {
  //  if (this.requisitionIdList.length) {
  //    this.labBLService.PutDoctor(this.doctorSelected.EmployeeId, this.requisitionIdList)
  //      .subscribe(res => {
  //        if (res.Status != "OK") {
  //          this.messageBoxService.showMessage("failed", ["Unable to update Doctor"]);
  //          console.log(res.ErrorMessage)
  //        } else {
  //          if (this.templateReport.ReportId) {
  //            this.labBLService.PutDoctorNameInLabReport(this.templateReport.ReportId, this.doctorSelected.LongSignature)
  //              .subscribe(res => {
  //                if (res.Status == "OK") {
  //                  //this.messageBoxService.showMessage("success", ["Doctor Name Updated in your Lab Report"]);
  //                } else {
  //                  this.messageBoxService.showMessage("failed", ["Doctor Name cannot be Updated in your Lab Report"]);
  //                }
  //              });
  //          }
  //          this.templateReport.Lookups.ReferredBy = this.doctorSelected.LongSignature;
  //          this.templateReport.Lookups.ReferredById = this.doctorSelected.EmployeeId;
  //          this.messageBoxService.showMessage("success", ["Doctor Updated"]);
  //        }
  //      });
  //  }
  //  else {
  //    this.messageBoxService.showMessage("failed", ["There are no requisitions !!"]);
  //  }

  //}

  UpdateDoctor() {
    if (this.requisitionIdList.length) {
      this.labBLService
        .PutDoctor(this.selectedPrescriberId, this.requisitionIdList)
        .subscribe((res) => {
          if (res.Status != "OK") {
            this.messageBoxService.showMessage("failed", ["Unable to update Doctor"]);
            console.log(res.ErrorMessage);
          } else {
            if (this.templateReport.ReportId) {
              this.labBLService
                .PutDoctorNameInLabReport(
                  this.templateReport.ReportId,
                  this.selectedPrescriberName
                )
                .subscribe((res) => {
                  if (res.Status == "OK") {
                    this.messageBoxService.showMessage("success", [
                      "Doctor Name Updated in your Lab Report",
                    ]);
                  } else {
                    this.messageBoxService.showMessage("failed", [
                      "Doctor Name cannot be Updated in your Lab Report",
                    ]);
                  }
                });
            }
            this.templateReport.Lookups.PrescriberName = this.selectedPrescriberName;
            this.templateReport.Lookups.PrescriberId = this.selectedPrescriberId;
            this.messageBoxService.showMessage("success", ["Doctor Updated"]);
          }
        });
    } else {
      this.messageBoxService.showMessage("failed", ["There are no requisitions !!"]);
    }
  }

  //makeDoctorEdit() {
  //  if (this.enableDrEdit) {
  //    this.enableDrEdit = true;
  //    this.changeDetector.detectChanges();
  //    this.enableDrEdit = false;
  //    if (this.templateReport.ReportId && (this.oldName != this.templateReport.Lookups.ReferredBy)) {
  //      if (this.templateReport.Lookups.ReferredBy.trim().length == 0) {
  //        this.templateReport.Lookups.ReferredBy = "SELF";
  //      }
  //      this.labBLService.PutDoctorNameInLabReport(this.templateReport.ReportId, this.templateReport.Lookups.ReferredBy)
  //        .subscribe(res => {
  //          if (res.Status == "OK") {
  //            this.oldName = this.templateReport.Lookups.ReferredBy;
  //            this.messageBoxService.showMessage("success", ["Doctor Name Updated in your Lab Report"]);
  //          } else {
  //            this.messageBoxService.showMessage("failed", ["Doctor Name cannot be Updated in your Lab Report"]);
  //          }
  //        });
  //    }
  //  }
  //  else {
  //    this.enableDrEdit = true;

  //    //Important to use detect Change otherwise cannot give the focus to input
  //    this.changeDetector.detectChanges();
  //    document.getElementById("docNameTextBox").blur();
  //    document.getElementById("docNameTextBox").focus();

  //  }
  //}

  //Code for Selective Printing Starts Here
  //called when user clicks the selective Print on the testname with More than one component in it
  public ShowHidePrintForTestsWithComp(
    template: LabResult_TemplatesVM,
    test: LabResult_TestVM
  ) {
    //set all the components contained by this test to either select or unselect based on select/unselect of Test
    test.Components.forEach((comp) => {
      comp["Print"] = test["Print"];
    });

    this.CheckForAllTemplateShowHide(template);
  }

  public ShowHidePrintForTestsWithoutComp(
    template: LabResult_TemplatesVM,
    test: LabResult_TestVM,
    len: number
  ) {
    //Select and unselect of the test with only one component
    if (len == 1) {
      test["Print"] = !test["Print"];
      this.CheckForAllTemplateShowHide(template);
    } else {
      //Select and unselect of the test with multiple component
      this.ShowHideComponentsToPrint(template, test);
    }
  }

  public ShowHideComponentsToPrint(
    template: LabResult_TemplatesVM,
    test: LabResult_TestVM
  ) {
    test["Print"] = false;

    test.Components.forEach((val) => {
      if (val["Print"] == true) {
        test["Print"] = true;
      }
    });

    this.CheckForAllTemplateShowHide(template);
  }

  public CheckForAllTemplateShowHide(template: LabResult_TemplatesVM) {
    template["Print"] = false;
    template.Tests.forEach((val) => {
      if (val["Print"] == true) {
        template["Print"] = true;
      }
    });
  }
  //Code for Selective Printing Ends Here

  //ashim: 20Sep018: added for update run number feature.
  ConfirmChangeRunNumber() {
    var createNew: boolean = window.confirm(
      "Are you sure to change Run Number?"
    );
    if (createNew) {
      this.visitType = this.templateReport.Lookups.VisitType;
      this.RunNumberType = this.templateReport.Lookups.RunNumberType;

      this.sampleCode.RunNumber = Number(
        this.templateReport.Lookups.SampleCode
      );

      this.sampleCode.SampleCreatedOn = moment(
        this.templateReport.Lookups.SampleDate
      ).format("YYYY-MM-DD");

      let nepaliDate = this.npCalendarService.ConvertEngToNepDate(
        this.templateReport.Lookups.SampleDate
      );

      if (nepaliDate) {
        this.sampleCode.SampleCode = nepaliDate.Day;
      }
      this.showChangeSample = true;
    }
  }
  //ashim: 20Sep018: added for update run number feature.
  SampleDateChanged() {
    let nepaliDate = this.npCalendarService.ConvertEngToNepDate(
      this.sampleCode.SampleCreatedOn
    );
    if (nepaliDate) {
      if (this.RunNumberType && this.RunNumberType.toLowerCase() == "normal") {
        this.sampleCode.SampleCode = nepaliDate.Day;
      } else {
        this.sampleCode.SampleCode = parseInt(
          nepaliDate.Year.toString().substring(1, 4)
        );
      }
    }
  }
  //ashim: 20Sep018: added for update run number feature.
  CheckIfSampleCodeExist() {
    if (this.CheckSampleCodeValidation()) {
      this.labBLService
        .GetSampleCodeCompared(
          this.sampleCode.RunNumber,
          this.visitType,
          this.sampleCode.SampleCreatedOn,
          this.RunNumberType,
          true
        )
        .subscribe((res) => {
          if (res.Status == "OK" && res.Results) {
            if (res.Results.Exist) {
              this.sampleCodeExistingDetail = res.Results;
              this.showConfirmationBox = true;
            } else {
              this.UpdateSampleCode();
            }
          }
        });
    }
  }
  //ashim: 20Sep018: added for update run number feature.
  CheckSampleCodeValidation(): boolean {
    let valid: boolean = false;
    if (this.sampleCode.RunNumber) {
      //check if user has selected future date.
      let checkFuture = moment(
        moment(this.sampleCode.SampleCreatedOn).format("YYYY-MM-DD")
      ).diff(moment().format("YYYY-MM-DD"));
      if (checkFuture <= 0) {
        var checkToday = moment(
          moment(this.sampleCode.SampleCreatedOn).format("YYYY-MM-DD")
        ).diff(
          moment(this.templateReport.Lookups.SampleDate).format("YYYY-MM-DD")
        );
        //if user don't change sample code or date and press OK
        if (
          this.sampleCode.RunNumber ==
          Number(this.templateReport.Lookups.SampleCode) &&
          checkToday == 0
        ) {
          this.showChangeSample = false;
        } else {
          valid = true;
        }
      } else {
        this.messageBoxService.showMessage("failed", [
          "Select valid sample collection date.",
        ]);
      }
    } else {
      this.messageBoxService.showMessage("failed", ["Enter valid run number."]);
    }
    return valid;
  }
  //ashim: 20Sep018: added for update run number feature.
  UpdateSampleCode() {
    var type: string = null;

    this.labBLService
      .PutSampleCodeReqIdList(
        this.requisitionIdList,
        this.sampleCode.RunNumber,
        this.sampleCode.SampleCreatedOn,
        this.visitType,
        this.RunNumberType
      )
      .subscribe((res) => {
        if (res.Status == "OK") {
          this.messageBoxService.showMessage("success", [
            "Sample code updated successfully.",
          ]);
          this.templateReport.Lookups.SampleCode =
            this.sampleCode.RunNumber.toString();
          this.templateReport.Lookups.SampleCodeFormatted =
            res.Results.FormattedSampleCode;
          this.templateReport.Lookups.SampleDate =
            this.sampleCode.SampleCreatedOn;
          this.showChangeSample = false;
          this.showConfirmationBox = false;
        } else {
          this.showChangeSample = false;
          this.showConfirmationBox = false;
          this.messageBoxService.showMessage("failed", [
            "Unable to update run number.",
          ]);
        }
      });
  }

  UpdateSpecimen(test: LabResult_TestVM) {
    var specimen = test.Specimen;
    var reqId = test.RequisitionId;

    if (specimen && specimen.trim().length > 0) {
      this.labBLService.PutSpecimen(specimen, reqId).subscribe((res) => {
        if (res.Status == "OK") {
          test.Specimen = res.Results;
          this.messageBoxService.showMessage("success", [
            "Specimen updated successfully.",
          ]);
        } else {
          this.showChangeSample = false;
          this.showConfirmationBox = false;
          this.messageBoxService.showMessage("failed", ["Unable to update Specimen."]);
        }
      });
    } else {
      this.labBLService.GetSpecimen(reqId).subscribe((res) => {
        if (res.Status == "OK") {
          test.Specimen = res.Results;
        } else {
        }
      });
      this.messageBoxService.showMessage("failed", ["Please Enter Specimen"]);
    }
  }

  ShowHideInterpretationToPrint(test: LabResult_TestVM) { }
  SignatureSelection($event) {
    if ($event === true) {
      this.IsSignatureValid = true;
    }
    else {
      this.IsSignatureValid = false;
    }
  }
  Verify() {
    this.templateReport.Templates.forEach((val) => {
      val.Tests = val.Tests.filter((tst) => tst["Print"] == true);
    });

    if (this.CheckSignatoriesValidation()) {
      this.loading = true;
      let labReport: LabReport = new LabReport();
      if (this.templateReport.ReportId) {
        labReport.LabReportId = this.templateReport.ReportId;
      }
      let reportDates = [];
      this.templateReport.Templates.forEach((template) => {
        template.Tests.forEach((test) => {
          test.Components.forEach((comp) => {
            labReport.ComponentIdList.push(comp.TestComponentResultId);
            reportDates.push(new Date(comp.CreatedOn));
          });
        });
      });
      let rcvDates = [];
      rcvDates.push(new Date(this.templateReport.Lookups.SampleDate));

      this.templateReport.Lookups.ReceivingDate = labReport.ReceivingDate =
        moment(new Date(Math.min.apply(null, rcvDates))).format(
          "YYYY-MM-DD HH:mm"
        );
      this.signatories = JSON.stringify(this.templateReport.Signatories);

      if (this.showLoggedInUserSignatory || !this.LabSignatoryParameter.EnableSignatoriesEdit) {
        var currUser = { EmployeeId: 0, Signature: "", SignatoryImageName: "", Show: false, DisplaySequence: 1000 };
        currUser.EmployeeId = this.CreatedByUser.EmployeeId;
        currUser.Signature = this.CreatedByUser.LabSignature;
        currUser.SignatoryImageName = this.CreatedByUser.SignatoryImageName;
        currUser.DisplaySequence = this.CreatedByUser.DisplaySequence;
        let signObj = [];

        if (this.signatories)
          signObj = JSON.parse(this.signatories);
        var dupSign = signObj.find(
          (itm) => itm.EmployeeId == currUser.EmployeeId
        );
        //if currentLoggedInUser is not in the List then add It
        if (!dupSign) {
          signObj.push(currUser);
        }
        this.signatories = JSON.stringify(signObj);
      }

      labReport.Signatories = this.signatories;

      labReport.PatientId = this.templateReport.Lookups.PatientId;
      labReport.PrescriberName = this.templateReport.Lookups.PrescriberName;
      labReport.Comments = this.templateReport.Comments;
      labReport.VerificationEnabled = this.verificationEnabled;
      labReport.PreVerificationEnabled = this.PreVerificationEnabled;

      this.labBLService.VerifyAllLabTests(labReport).subscribe((res) => {
        if (res.Status === ENUM_DanpheHTTPResponseText.OK) {
          // if (
          //   this.routeAfterVerification &&
          //   this.routeAfterVerification.trim() &&
          //   this.routeAfterVerification.trim().length > 0
          // ) {
          //   let route = "/Lab/" + this.routeAfterVerification;
          //   this.loading = false;
          //   this.router.navigate([route]);
          //   return;
          // }
          this.messageBoxService.showMessage(ENUM_MessageBox_Status.Success, [`Report is successfully verified...`]);
          this.callbackAddUpdate.emit({ verified: true });
        } else {
          this.messageBoxService.showMessage("failed", [
            "Unable to Verify the report.",
          ]);
          console.log(res.ErrorMessage);
          this.loading = false;
        }
      });
    }
  }

  //public ShowHideDigitalImage(ind: number) {
  //  var imageSelector;
  //  var showBtn;
  //  var hideBtn;
  //  if (ind > -1) {
  //    imageSelector = document.getElementById('signImage' + ind);
  //    showBtn = document.getElementById('showSignImage' + ind);
  //    hideBtn = document.getElementById('hideSignImage' + ind);
  //  }
  //  else {
  //    imageSelector = document.getElementById('currUserSignImage');
  //    showBtn = document.getElementById('showCurrUserSignImage');
  //    hideBtn = document.getElementById('hideCurrUserSignImage');
  //  }

  //  if (imageSelector && imageSelector.style.display && (imageSelector.style.display == 'inline-block' || imageSelector.style.display == 'block')) {
  //    showBtn.style.display = 'inline-block';
  //    hideBtn.style.display = 'none';
  //    imageSelector.style.display = 'none';
  //  } else {
  //    showBtn.style.display = 'none';
  //    hideBtn.style.display = 'inline-block';
  //    imageSelector.style.display = 'inline-block';
  //  }
  //}

  public ShowHideDigitalImage(emp: any) {
    if (emp.Show) {
      emp.Show = false;
    } else {
      emp.Show = true;
    }
  }

  public EditSignatories() {
    this.signatories = JSON.stringify(this.templateReport.Signatories);
    this.showSignatoriesEdit = true;
  }

  //prat: 20sep2019 for internal and external referrer
  selectedPrescriberId: number = null;
  selectedPrescriberName: string = null;

  OnReferrerChanged($event) {
    this.selectedPrescriberId = $event.ReferrerId; //EmployeeId comes as ReferrerId from select referrer component.
    this.selectedPrescriberName = $event.ReferrerName; //EmployeeName comes as ReferrerName from select referrer component.
  }

  public ExtRefSettings = {
    EnableExternal: true,
    DefaultExternal: false,
    AllowFreeText: false,
  };

  public LoadReferrerSettings() {
    var currParam = this.coreService.Parameters.find(
      (a) =>
        a.ParameterGroupName == "Lab" &&
        a.ParameterName == "ExternalReferralSettings"
    );
    if (currParam && currParam.ParameterValue) {
      this.ExtRefSettings = JSON.parse(currParam.ParameterValue);
    }
  }
  //end: Pratik: 20Sept'19--For External Referrals

  public callBackUpload() {
    this.callBackUplaod.emit();
    setTimeout(() => {
      this.loading = false;
    }, 500);
  }

  GetLabReportDisplayParam(): void {
    let param = this.coreService.Parameters.find(a => a.ParameterGroupName === 'LAB' && a.ParameterName === 'LabReportDisplaySettings');
    if (param)
      this.LabReportDisplayParameter = JSON.parse(param.ParameterValue);
    else
      this.messageBoxService.showMessage(ENUM_MessageBox_Status.Error, ["Please enter parameter values for LabReportDisplaySettings"]);

    let signatoryParam = this.coreService.Parameters.find(a => a.ParameterGroupName === "LAB" && a.ParameterName === "LabSignatorySettings");
    if (signatoryParam) {
      this.LabSignatoryParameter = JSON.parse(signatoryParam.ParameterValue);
    }
  }
}
