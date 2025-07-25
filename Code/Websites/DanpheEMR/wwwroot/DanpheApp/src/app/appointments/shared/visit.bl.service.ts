import { Injectable } from '@angular/core';
import * as _ from 'lodash';
import * as cloneDeep from 'lodash/cloneDeep';
import * as moment from 'moment/moment';
import { BillInvoiceReturnModel } from '../../billing/shared/bill-invoice-return.model';
import { BillItemRequisition } from '../../billing/shared/bill-item-requisition.model';
import { BillingTransaction } from '../../billing/shared/billing-transaction.model';
import { BillingDLService } from '../../billing/shared/billing.dl.service';
import { ClaimBookingRoot_DTO } from '../../claim-management/shared/SSF-Models';
import { Employee } from '../../employee/shared/employee.model';
import { ClaimRoot } from '../../insurance/ssf/shared/SSF-Models';
import { PatientsDLService } from '../../patients/shared/patients.dl.service';
import { SecurityService } from '../../security/shared/security.service';
import { ENUM_AppointmentType, ENUM_BillingStatus, ENUM_VisitStatus, ENUM_VisitType } from '../../shared/shared-enums';
import { AppointmentDLService } from './appointment.dl.service';
import { QuickVisitVM } from './quick-visit-view.model';
import { VisitDLService } from './visit.dl.service';
import { Visit } from './visit.model';


@Injectable()
export class VisitBLService {

  constructor(public visitDLService: VisitDLService,
    public billingDLService: BillingDLService,
    public appointmentDLService: AppointmentDLService,
    public patientDLService: PatientsDLService,
    public securityService: SecurityService) { }

  //gets all the visit of a patient
  public GetPatientVisits(patientId: number) {
    return this.visitDLService.GetPatientVisitList(patientId)
      .map(res => res);
  }

  public GetPatientVisitList(patientId: number) {
    return this.visitDLService.GetPatientVisitList(patientId)
      .map(res => res);
  }

  public GetPatientVisits_Today(patientId: number) {
    return this.visitDLService.GetPatientVisitList_Today(patientId)
      .map(res => res);
  }

  // public GetPatientVisitEarlierList(patientId: number, followup: boolean) {
  //   return this.visitDLService.GetPatientVisitEarlierList(patientId, followup)
  //     .map(res => res);
  // }

  // public GetVisitList(claimCode: number, patId: number) {
  //   return this.visitDLService.GetVisitList(claimCode, patId)
  //     .map(res => res);
  // }

  public GetPatientById(patientId: number) {
    return this.patientDLService.GetPatientById(patientId)
      .map(res => res);
  }
  // public GetAdditionalBillingItems() {
  //   return this.visitDLService.GetAdditionalBillingItems()
  //     .map(res => res);
  // }
  //post new visit
  // public AddVisit(currentVisit: Visit) {
  //   currentVisit.VisitStatus = ENUM_VisitStatus.initiated;// "initiated";
  //   var tempVisitModel = _.omit(currentVisit, ['VisitValidator']);
  //   return this.visitDLService.PostVisit(tempVisitModel)
  //     .map(res => res);
  // }
  //get visit list according to status
  // public GetVisitsByStatus(status: string, maxlimitdays: number, searchTxt) {
  //   //var status = "initiated";
  //   return this.visitDLService.GetVisitsByStatus(status, maxlimitdays, searchTxt)
  //     .map(res => res);
  // }

  //get visit list
  public GetVisits(maxlimitdays: number, searchTxt, IsHospitalNoSearch, IsIdCardNoSearch) {
    //var status = "initiated";
    return this.visitDLService.GetVisits(maxlimitdays, searchTxt, IsHospitalNoSearch, IsIdCardNoSearch)
      .map(res => res);
  }

  public GetDoctorOpdPrices() {
    return this.visitDLService.GetDoctorOpdPrices()
      .map(res => res);
  }
  //mapping to BillItemRequistion model and posting to BillingRequisitionItem Table
  //requested by is the employeeid of the frontdesk user.
  public PostToBilling(visit: Visit, requestedBy: number) {
    let currentUser: number = 1;//logged in receptionist id--needs revision
    let billItems: Array<BillItemRequisition> = new Array<BillItemRequisition>();
    billItems.push({
      BillItemRequisitionId: 0,
      ItemId: visit.PerformerId,//itemid is set as providerid, needs revision.
      RequisitionId: visit.PatientVisitId,
      ProcedureCode: visit.PerformerId.toString(),
      //RequestedBy: requestedBy,
      PatientId: visit.PatientId,
      PatientVisitId: visit.PatientVisitId,
      ServiceDepartment: "OPD", //service department OPD by default for now
      DepartmentName: "OPD",
      ServiceDepartmentId: 0,
      Quantity: 1,
      PerformerId: visit.PerformerId,
      CreatedBy: this.securityService.GetLoggedInUser().EmployeeId,
      CreatedOn: moment().format('YYYY-MM-DD'),
      ItemName: null,
      Price: 0,////check for proper price and change it later.
      AssignedTo: visit.PerformerId //need to change this later on.. sud: 20may
    });

    return this.billingDLService.PostBillingItemRequisition(billItems)
      .map(bill => bill);
  }

  //once visit is created updating the appointment status
  public UpdateAppointmentStatus(appointmentId: number, status: string, performerId: number, performerName: string) {
    return this.appointmentDLService.PutAppointmentStatus(appointmentId, status, performerId, performerName)
      .map((responseData) => {
        return responseData;
      });
  }
  public GetVisitInfoforStickerPrint(PatientVisitId: number) {
    return this.visitDLService.GetVisitInfoforStickerPrint(PatientVisitId)
      .map((responseData) => {
        return responseData;
      });
  }


  public GetDoctorList(departmentId: number) {
    return this.appointmentDLService.GetDoctorFromDepartmentId(departmentId)
      .map(res => res);
  }
  public GetDepartmentList() {
    return this.visitDLService.GetDepartmentList()
      .map(res => res);
  }
  public GetDepartmentByEmployeeId(employeeId: number) {
    return this.visitDLService.GetDepartmentByEmployeeId(employeeId)
      .map(res => res);
  }

  public ContinueNextVisit(visit: Visit, referredProvider: Employee, continuationType: string) {
    visit.ReferredById = continuationType == "followup" ? null : visit.PerformerId;
    visit.ParentVisitId = visit.PatientVisitId;
    visit.PriceCategoryId = visit.PriceCategoryId;

    visit.PerformerId = referredProvider ? referredProvider.EmployeeId : 0;
    visit.PerformerName = referredProvider ? referredProvider.FullName : null;
    visit.AppointmentType = continuationType;


    visit.VisitDate = moment().format('YYYY-MM-DD');
    visit.VisitTime = moment().add((5 - moment().minute() % 5), 'minutes').format('HH:mm');
    visit.VisitCode = null;
    visit.IsVisitContinued = false;
    visit.IsActive = true;
    visit.BillingStatus = ENUM_BillingStatus.paid;// 'paid';
    visit.VisitStatus = ENUM_VisitStatus.initiated;// 'initiated';
    visit.VisitDuration = 0;
    //added createdon and createdby for referral visit--sud:19Aug
    visit.CreatedOn = moment().format('YYYY-MM-DD HH:mm:ss');
    visit.CreatedBy = this.securityService.GetLoggedInUser().EmployeeId;
    //used to post billingtransaction during transfer and referral
    visit.CurrentCounterId = this.securityService.getLoggedInCounter().CounterId;
    var tempVisitModel = _.omit(visit, ['VisitValidator', 'Patient']);
    return this.visitDLService.PostTransferVisit(tempVisitModel)
      .map(res => res)
  }

  //Below Added by Nagesh
  //get doctors list using department Id
  public GenerateDoctorList(departmentId: number) {
    return this.visitDLService.GetDoctorFromDepartmentId(departmentId)
      .map(res => res);
  }
  // getting the CountrySubDivision from dropdown
  public GetCountrySubDivision(countryId: number) {
    return this.patientDLService.GetCountrySubDivision(countryId)
      .map(res => { return res })
  }
  public GetCountries(countryId: number) {
    return this.patientDLService.GetCountries()
      .map(res => { return res })
  }
  //getting membership deatils by membershiptype id
  public GetMembershipDeatilsByMembershipTyepId(membershipTypeId) {
    return this.visitDLService.GetMembershipDeatilsByMembershipTyepId(membershipTypeId)
      .map(res => { return res });
  }

  //It's no need -Nagesh
  //gets providers availablity using visitDate and ProviderId.
  //public ShowProviderAvailability(selProviderId: number, visitDate: string) {
  //    if ((visitDate != "" && visitDate != null) && (selProviderId != 0 && selProviderId != null)) {
  //        return this.visitDLService.GetProviderAvailability(selProviderId, visitDate)
  //            .map(res => res);
  //    }
  //    else {
  //        alert("select correct date and/or provider.");
  //    }
  //}

  // //getting total amoutn opd by doctorId
  // public GetTotalAmountByProviderId(providerId) {
  //   return this.visitDLService.GetTotalAmountByProviderId(providerId)
  //     .map(res => { return res });

  // }
  //Post Visit data to Database with Patient, BillTransaction, BillTransactionItems and Patient details
  public PostVisitToDB(currentVisit: QuickVisitVM) {

    let clonedObject = cloneDeep(currentVisit);
    var visitData = _.omit(currentVisit, [
      'QuickAppointmentValidator',
      'Patient.PatientValidator',
      'Visit.VisitValidator',
      'Patient.PatientScheme.PatientSchemeValidator',
      'Patient.Guarantor',
      'Patient.CountrySubDivision',
    ]);


    let txnItms = currentVisit.BillingTransaction.BillingTransactionItems.map(itm => {
      return _.omit(itm, ['BillingTransactionItemValidator', 'Patient']);
    });
    var currVisit = visitData;
    currVisit.BillingTransaction.BillingTransactionItems = txnItms;
    if (currVisit.Patient.PatientScheme.OpCreditLimit === null) {
      currVisit.Patient.PatientScheme.OpCreditLimit = 0
    }

    if (currVisit.Patient.PatientScheme.IpCreditLimit === null) {
      currVisit.Patient.PatientScheme.IpCreditLimit = 0
    }

    if (currVisit.Patient.PatientScheme.GeneralCreditLimit === null) {
      currVisit.Patient.PatientScheme.GeneralCreditLimit = 0
    }

    let visDataJson = JSON.stringify(currVisit);


    //Once we create the Json, we need to reassign the validators since it'll give Form-Instance 'control' not defined error
    //when server responds with Failed status.
    currentVisit.Patient.PatientValidator = clonedObject.Patient.PatientValidator;
    currentVisit.Visit.VisitValidator = clonedObject.Visit.VisitValidator;
    currentVisit.Patient.PatientScheme.PatientSchemeValidator = clonedObject.Patient.PatientScheme.PatientMapPriceCategoryValidator;


    //currentVisit.QuickAppointmentValidator = clonedObject.QuickAppointmentValidator;

    return this.visitDLService.PostVisitToDB(visDataJson)
      .map(res => res);
  }


  //Get Matching Patient Details by FirstName,LastName,PhoneNumber for showing registered matching patient on Visit Creation time
  public GetExistedMatchingPatientList(FirstName, LastName, PhoneNumber, Age, Gender, IsInsurance = false, IMISCode = null) {
    return this.patientDLService.GetExistedMatchingPatientList(FirstName, LastName, PhoneNumber, Age, Gender, IsInsurance, IMISCode)
      .map(res => { return res });
  }

  public GetApptForDeptOnSelectedDate(deptId, doctorId, selectedDate, patientId) {
    return this.visitDLService.GetApptForDeptOnSelectedDate(deptId, doctorId, selectedDate, patientId)
      .map(res => { return res });
  }

  //ashim: 17Aug'2018
  //this function is used in return visit billing during transfer visit case.
  public PostReturnTransaction(billingTransaction: BillingTransaction, returnRemarks: string) {
    let input = new FormData();

    let returnReceipt = new BillInvoiceReturnModel();
    returnReceipt.RefInvoiceNum = billingTransaction.InvoiceNo;
    returnReceipt.PatientId = billingTransaction.PatientId;
    returnReceipt.BillingTransactionId = billingTransaction.BillingTransactionId;
    returnReceipt.SubTotal = billingTransaction.SubTotal;
    returnReceipt.DiscountAmount = billingTransaction.DiscountAmount;
    returnReceipt.TaxableAmount = billingTransaction.TaxableAmount;
    returnReceipt.TaxTotal = billingTransaction.TaxTotal;
    returnReceipt.TotalAmount = billingTransaction.TotalAmount;
    returnReceipt.Remarks = returnRemarks;
    returnReceipt.CounterId = this.securityService.getLoggedInCounter().CounterId;
    returnReceipt.IsActive = true;
    returnReceipt.InvoiceCode = billingTransaction.InvoiceCode;
    returnReceipt.TaxId = billingTransaction.TaxId;
    returnReceipt.Tender = billingTransaction.Tender;

    var data = JSON.stringify(returnReceipt);
    input.append("billInvReturnModel", data);


    return this.billingDLService.PostReturnReceipt(input)
      .map(res => res);
  }
  // public GetHealthCardBillItem() {
  //   return this.billingDLService.GetHealthCardBillItem().map(res => {
  //     return res
  //   });
  // }

  public GetBillTxnByRequisitionId(requisitionId: number, patientId: number) {
    return this.billingDLService.GetBillTxnByRequisitionId(requisitionId, patientId, "OPD")
      .map(res => res);
  }

  public GetMemberShipTypes() {
    return this.patientDLService.GetMembershipTypes()
      .map(res => res);
  }
  public GetPatHealthCardStatus(patId: number) {
    return this.visitDLService.GetPatHealthCardStatus(patId)
      .map(res => res);
  }
  public GetPatientBillingContext(patientId: number) {
    return this.billingDLService.GetPatientBillingContext(patientId)
      .map(res => res);
  }

  public PostFreeReferralVisit(visit: Visit) {
    var visitData = _.omit(visit, [
      'VisitValidator'
    ]);




    let visitJson = JSON.stringify(visitData);
    return this.visitDLService.PostFreeReferralVisit(visitJson);
  }


  //sud: 19June'19--For Department OPD
  public GetDepartmentOpdItems() {
    return this.visitDLService.GetDepartmentOpdItems();
  }

  //sud: 21June'19--For Department followup
  public GetDepartmentFollowupItems() {
    return this.visitDLService.GetDepartmentFollowupItems();
  }


  //sud: 21June'19--For Doctor followup
  public GetDoctorFollowupItems() {
    return this.visitDLService.GetDoctorFollowupItems();
  }

  //sud: 31Jul'19-For Old Patient Opd
  public GetDoctorOldPatientPrices() {
    return this.visitDLService.GetDoctorOldPatientPrices();
  }
  public GetDoctorReferralPatientPrices() {
    return this.visitDLService.GetDoctorReferralPatientPrices();
  }

  // getting department list
  public GetDepartment() {
    return this.appointmentDLService.GetDepartment()
      .map(res => { return res })

  }

  //sud: 31Jul'19-For Old Patient Opd
  public GetDepartmentOldPatientPrices() {
    return this.visitDLService.GetDepartmentOldPatientPrices();
  }

  public GetVisitDoctors() {
    return this.visitDLService.GetVisitDoctors();
  }

  public GetRequestingDepartmentByVisitId(visitId: number) {
    return this.visitDLService.GetRequestingDepartmentByVisitId(visitId);
  }

  public GetBillItemList() {
    return this.visitDLService.GetBillItemList();
  }
  public PostFreeFollowupVisit(fwUpVisit: Visit, parentVisitId: number, isManualFreeFollowup: boolean = false) {

    let fwUpVisToPost = new Visit();
    fwUpVisToPost.PatientId = fwUpVisit.PatientId;
    fwUpVisToPost.PerformerId = fwUpVisit.PerformerId;
    fwUpVisToPost.PerformerName = fwUpVisit.PerformerName;
    fwUpVisToPost.DepartmentId = fwUpVisit.DepartmentId;
    fwUpVisToPost.AppointmentType = ENUM_AppointmentType.followup;
    fwUpVisToPost.VisitType = ENUM_VisitType.outpatient;
    fwUpVisToPost.VisitStatus = ENUM_VisitStatus.initiated;
    fwUpVisToPost.ParentVisitId = parentVisitId;
    fwUpVisToPost.PriceCategoryId = fwUpVisit.PriceCategoryId;
    fwUpVisToPost.SchemeId = fwUpVisit.SchemeId;

    fwUpVisToPost.VisitDate = moment().format('YYYY-MM-DD');
    fwUpVisToPost.VisitTime = moment().add((5 - moment().minute() % 5), 'minutes').format('HH:mm');
    fwUpVisToPost.IsActive = true;
    fwUpVisToPost.BillingStatus = ENUM_BillingStatus.free;//sud:9Aug
    fwUpVisToPost.VisitDuration = 0;

    //added createdon and createdby for fwup visit-sud:26une'19
    fwUpVisToPost.CreatedOn = moment().format('YYYY-MM-DD HH:mm:ss');
    fwUpVisToPost.CreatedBy = this.securityService.GetLoggedInUser().EmployeeId;
    fwUpVisToPost.IsManualFreeFollowup = isManualFreeFollowup;

    //used to post billingtransaction during transfer and referral
    fwUpVisToPost.CurrentCounterId = this.securityService.getLoggedInCounter().CounterId;
    var tempVisitModel = _.omit(fwUpVisToPost, ['VisitValidator']);
    return this.visitDLService.PostFreeFollowupVisit(tempVisitModel);
  }


  //Post Visit data to Database with Patient, BillTransaction, BillTransactionItems and Patient details
  public PostPaidFollowupVisit(fwupVisit: QuickVisitVM) {


    let clonedObject = cloneDeep(fwupVisit);

    var visitData = _.omit(fwupVisit, [
      'QuickAppointmentValidator',
      'Patient.PatientValidator',
      'Visit.VisitValidator',
      'Patient.Guarantor',
      'Patient.CountrySubDivision',
      'Patient.PatientScheme.PatientSchemeValidator'
    ]);

    let txnItms = fwupVisit.BillingTransaction.BillingTransactionItems.map(itm => {
      return _.omit(itm, ['BillingTransactionItemValidator', 'Patient']);
    });

    var currVisit = visitData;
    currVisit.BillingTransaction.BillingTransactionItems = txnItms;


    let visDataJson = JSON.stringify(currVisit);
    //Once we create the Json, we need to reassign the validators since it'll give Form-Instance 'control' not defined error
    //when server responds with Failed status.
    fwupVisit.Patient.PatientValidator = clonedObject.Patient.PatientValidator;
    fwupVisit.Visit.VisitValidator = clonedObject.Visit.VisitValidator;

    return this.visitDLService.PostPaidFollowupVisit(visDataJson)
      .map(res => res);

  }

  public GetMunicipality(id: number) {
    return this.patientDLService.GetMunicipality(id)
      .map(res => { return res })
  }

  public updateVisitStatusInTelemedicine(url: string, visitId: string, visitStatus: string) {
    return this.appointmentDLService.updateVisitStatusInTelemedicine(url, visitId, visitStatus).map(res => { return res });
  }

  public updatePaymentStatusInTelMed(url: string, visitId: string) {
    return this.appointmentDLService.updatePaymentStatus(url, visitId).map(res => { return res });
  }

  public GetAPIPatientDetail(url: string, IDCardNumber: string) {
    return this.visitDLService.GetAPIPatientDetail(url, IDCardNumber).map(res => {
      return res
    });
  }

  public GetDependentIdDetail(DependentId: string) {
    return this.visitDLService.GetDependentIdDetail(DependentId).map(res => {
      return res
    });
  }

  public UpdateDependentId(DependentId: string, PatientId: number) {
    return this.visitDLService.UpdateDependentId(DependentId, PatientId).map(res => {
      return res
    });
  }

  public GetSSFPatientDetail(PatientId: string) {
    return this.visitDLService.GetSSFPatientDetail(PatientId).map(res => {
      return res;
    });
  }

  public CheckEligibility(PatientId: string, VisitDate: string) {
    return this.visitDLService.CheckEligibility(PatientId, VisitDate).map(res => {
      return res;
    });
  }

  public GetSSFEmployerDetail(SSFPatientGUID: string) {
    return this.visitDLService.GetSSFEmployerDetail(SSFPatientGUID).map(res => {
      return res;
    });
  }

  public GetSSFInvoiceDetail(fromDate: string, toDate: string, patientType: string) {
    return this.visitDLService.GetSSFInvoiceDetail(fromDate, toDate, patientType).map(res => {
      return res;
    });
  }

  public SubmitClaim(ClaimRoot: ClaimRoot) {
    return this.visitDLService.SubmitClaim(ClaimRoot).map(res => {
      return res;
    });
  }
  public GetClaimBookingDetails(claimCode: number) {
    return this.visitDLService.GetClaimBookingDetails(claimCode).map(res => {
      return res;
    });
  }
  public BookClaim(claimBookingObj: ClaimBookingRoot_DTO) {
    return this.visitDLService.BookClaim(claimBookingObj).map(res => {
      return res;
    });
  }
  public GetRank() {
    return this.visitDLService.GetRank().map(res => {
      return res;
    })
  }
  public PostRank(RankName: string) {
    return this.visitDLService.PostRank(RankName).map(res => {
      return res;
    })
  }
  public IsClaimed(latestClaimCode: number, patientId: number) {
    return this.visitDLService.IsClaimed(latestClaimCode, patientId).map(res => {
      return res;
    });
  }
  public getSSFPatientDetailLocally(patientId: number, schemeId: number = null) {
    return this.visitDLService.getSSFPatientDetailLocally(patientId, schemeId).map(res => {
      return res;
    });
  }
  public GetSSFSubProduct(claimCode: number) {
    return this.visitDLService.GetSSFSubProduct(claimCode).map(res => {
      return res;
    });
  }

  public getMedicareMemberDetail(patientId: number) {
    return this.visitDLService.getMedicareMemberDetail(patientId).map(res => {
      return res;
    });
  }

  public getSSFPatientDetailAndCheckSSFEligibilityFromSsfServer(policyNo: string, currentDate: string) {
    return this.visitDLService.getSSFPatientDetailAndCheckSSFEligibilityFromSsfServer(policyNo, currentDate).map(res => {
      return res;
    });
  }

  public getMemberInfoByScheme(schemeId: number, patientId: number) {
    return this.visitDLService.getMemberInfoByScheme(schemeId, patientId).map(res => {
      return res;
    });
  }
  public GetPatientCreditLimitsByScheme(schemeId: number, patientId: number, serviceBillingContext: string) {
    return this.visitDLService.GetPatientCreditLimitsByScheme(schemeId, patientId, serviceBillingContext).map(res => {
      return res;
    });
  }

  public GetLatestClaimCodeForAutoGeneratedClaimCodes(creditOrganizationId: number) {
    return this.visitDLService.GetLatestClaimCodeForAutoGeneratedClaimCodes(creditOrganizationId).map(res => {
      return res;
    });
  }
  public GetPatientLastVisitDetail(patientId: number) {
    return this.visitDLService.GetPatientLastVisitDetail(patientId).map(res => {
      return res;
    });
  }

  public GetPatientVisitsDetails(patientId: number) {
    return this.visitDLService.GetPatientVisitsDetails(patientId).map(res => {
      return res;
    });
  }
  public GetPatientVisitsBySearchFilterOption(filterBy: string, searchTxt: string) {
    return this.visitDLService.GetPatientVisitsBySearchFilterOption(filterBy, searchTxt)
      .map(res => res);
  }
}
