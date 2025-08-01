import { Injectable } from '@angular/core';
import * as _ from 'lodash';
import * as moment from 'moment/moment';
import { ADT_DLService } from '../../adt/shared/adt.dl.service';
import { AppointmentDLService } from '../../appointments/shared/appointment.dl.service';
import { VisitDLService } from '../../appointments/shared/visit.dl.service';
import { ClinicalDLService } from '../../clinical/shared/clinical.dl.service';
import { LabsDLService } from '../../labs/shared/labs.dl.service';
import { ImagingDLService } from '../../radiology/shared/imaging.dl.service';
import { PatientFilesModel } from './patient-files.model';
import { MedicareDependentModel } from './patient-medicare-dependent.model';
import { MedicareMemberModel } from './patient-medicare-member.model';
import { Patient } from './patient.model';
import { PatientsDLService } from './patients.dl.service';

//Note: mapping is done here by blservice, component will only do the .subscribe().
@Injectable()
export class PatientsBLService {

  //we-re gradually moving business logic from component to BLServices


  constructor(public patientDLService: PatientsDLService,
    public appointmentDLService: AppointmentDLService,
    public visitDLService: VisitDLService,
    public labsDLService: LabsDLService,
    public imagingDLService: ImagingDLService,
    public clinicalDLService: ClinicalDLService,
    public admissionDLService: ADT_DLService) {

  }

  public GetSalutationList() {
    return this.patientDLService.GetSalutationList()
      .map(res => { return res; });
  }
  public GetMedicarePatientList() {
    return this.patientDLService.GetMedicarePatients()
      .map(res => res);
  }
  public GetAllDepartment() {
    return this.patientDLService.GetAllDepartments()
      .map(res => res);
  }

  public GetAllDesignations() {
    return this.patientDLService.GetAllDesignations()
      .map(res => res);
  }
  public GetAllMedicareTypes() {
    return this.patientDLService.GetAllMedicareTypes()
      .map(res => res);
  }
  public GetAllMedicareInstitutes() {
    return this.patientDLService.GetAllMedicareInstitutes()
      .map(res => res);
  }
  public GetAllInsuranceProviderList() {
    return this.patientDLService.GetAllInsuranceProviderList()
      .map(res => res);
  }
  public GetPatientsWithVisitsInfo(searchTxt) {
    return this.patientDLService.GetPatientsWithVisitsInfo(searchTxt)
      .map(res => res);
  }

  public GetMedicareMemberDetailByPatientId(patientId) {
    return this.patientDLService.GetMedicareMemberDetailByPatientId(patientId)
      .map(res => res);
  }
  public GetMedicareDependentMemberDetailByPatientId(patientId) {
    return this.patientDLService.GetMedicareDependentMemberDetailByPatientId(patientId)
      .map(res => res);
  }
  public GetMedicareMemberDetailByMedicareNumber(memberNo) {
    return this.patientDLService.GetMedicareMemberDetailByMedicareNumber(memberNo)
      .map(res => res);
  }
  public PostMedicareDependentDetails(medicareDepDetails: MedicareDependentModel) {
    let dependentDetails = _.omit(medicareDepDetails, ['MedicareDependentValidator']);
    return this.patientDLService.PostMedicareDependentDetails(dependentDetails)
      .map(res => res);
  }
  public PutMedicareDependentDetails(medicareDepDetails: MedicareDependentModel) {
    let dependentDetails = _.omit(medicareDepDetails, ['MedicareDependentValidator']);
    return this.patientDLService.PutMedicareDetails(dependentDetails)
      .map(res => res);
  }
  public PostMedicareMemberDetails(medicareMemDetails: MedicareMemberModel) {
    let memberDetails = _.omit(medicareMemDetails, ['MedicareMemberValidator']);
    return this.patientDLService.PostMedicareMemberDetails(memberDetails)
      .map(res => res);
  }
  public PutMedicareMemberDetails(medicareMemDetails: MedicareMemberModel) {
    let memberDetails = _.omit(medicareMemDetails, ['MedicareMemberValidator']);
    return this.patientDLService.PutMedicareDetails(memberDetails)
      .map(res => res);
  }

  // for getting the Patient
  public GetPatients(searcgTxt) {
    return this.patientDLService.GetPatients(searcgTxt)
      .map(res => { return res })
  }
  //  get registered patients
  public GetPatientsList(searcgTxt, searchUsingHospitalNo) {
    return this.patientDLService.GetPatientsList(searcgTxt, searchUsingHospitalNo)
      .map(res => { return res })
  }

  public GetPatientById(patientId: number) {
    return this.patientDLService.GetPatientById(patientId)
      .map(res => { return res })
  }
  // getting the CountrySubDivision from dropdown
  public GetCountrySubDivision(countryId: number) {
    return this.patientDLService.GetCountrySubDivision(countryId)
      .map(res => { return res })

  }
  public GetCountries() {
    return this.patientDLService.GetCountries()
      .map(res => { return res })
  }
  public GetMembershipType() {
    return this.patientDLService.GetMembershipType()
      .map(res => { return res })
  }
  public GetPatientBillHistory(patientCode: string) {
    return this.patientDLService.GetPatientBillHistory(patientCode)
      .map(res => { return res })
  }
  public GetPatientLabReport(patientId: number) {
    return this.labsDLService.GetPatientReport(patientId)
      .map(res => res);
  }
  public GetPatientImagingReports(patientId: number) {
    return this.imagingDLService.GetPatientReports(patientId, 'final')
      .map(res => res);
  }
  public GetPatientVisitList(patientId: number) {
    return this.visitDLService.GetPatientVisitList(patientId)
      .map(res => res);
  }
  public GetPatientDrugList(patientId: number) {
    return this.clinicalDLService.GetMedicationList(patientId)
      .map(res => res);
  }

  public getPatientUplodedDocument(patientId: number) {
    return this.patientDLService.GetPatientUplodedDocument(patientId)
      .map(res => res);
  }
  public GetAdmissionHistory(patientId: number) {
    return this.admissionDLService.GetAdmissionHistory(patientId)
      .map(res => res);
  }
  public GetLightPatientById(patientId: number) {
    return this.patientDLService.GetLightPatientById(patientId)
      .map(res => res);
  }
  public GetInsuranceProviderList() {
    return this.patientDLService.GetInsuranceProviderList()
      .map(res => res);
  }
  public GetDialysisCode() {
    return this.patientDLService.GetDialysisCode()
      .map(res => res);
  }
  // for posting the patient
  public PostPatient(patientObj: Patient) {
    //ommitting all validators, before sending to server.
    //BUT, guarantorValidator is behaving differently so we've created this work-around to
    // assign it back to the patientobject -- needs better approach later.. --sudarshan-27feb'17
    let guarValidator = patientObj.Guarantor.GuarantorValidator;

    var temp = _.omit(patientObj, ['PatientValidator',
      'Addresses[0].AddressValidator',
      'Addresses[1].AddressValidator',
      'Insurances[0].InsuranceValidator',
      'Insurances[1].InsuranceValidator',
      'KinEmergencyContacts[0].KinValidator',
      'KinEmergencyContacts[1].KinValidator',
      'Guarantor.GuarantorValidator',
      'ProfilePic.PatientFilesValidator',
      'CountrySubDivision', 'PatientScheme'
    ]);
    let data = JSON.stringify(temp);
    patientObj.Guarantor.GuarantorValidator = guarValidator;
    return this.patientDLService.PostPatient(data)
      .map(res => { return res })
  }
  //this returns an observable, calling client can subscribe to it..
  // for updating the patient
  public PutPatient(patientObj: Patient) {
    //do your business logic here, like removing the validator etc...
    let guarValidator = patientObj.Guarantor.GuarantorValidator;
    var temp = _.omit(patientObj, ['PatientValidator',
      'Addresses[0].AddressValidator',
      'Addresses[1].AddressValidator',
      'Insurances[0].InsuranceValidator',
      'Insurances[1].InsuranceValidator',
      'KinEmergencyContacts[0].KinValidator',
      'KinEmergencyContacts[1].KinValidator',
      'Guarantor.GuarantorValidator',
      'ProfilePic.PatientFilesValidator',
      'CountrySubDivision', 'PatientScheme',]);
    let data = JSON.stringify(temp);
    //ommitting and re-assigning the validator, which was behaving strangely
    patientObj.Guarantor.GuarantorValidator = guarValidator;
    //pass patientid and stringyfied object to the dlservice
    return this.patientDLService.PutPatient(patientObj.PatientId, data)
      .map(res => { return res })
  }
  //this is for apppointment modules
  public PutAppointmentPatientId(appointmentId: number, patientId: number) {
    return this.appointmentDLService.PutAppointmentPatientId(appointmentId, patientId)
      .map(res => { return res })
  }
  //Get Matching Patient Details by FirstName,LastName,PhoneNumber for showing registered matching patient on Registration Creation time
  public GetExistedMatchingPatientList(FirstName, LastName, PhoneNumber, Age, Gender, IsInsurance = false, IMISCode = null) {
    return this.patientDLService.GetExistedMatchingPatientList(FirstName, LastName, PhoneNumber, Age, Gender, IsInsurance, IMISCode)
      .map(res => { return res });
  }
  public AddPatientFiles(filesToUpload, patReport: PatientFilesModel) {
    try {
      let formToPost = new FormData();
      //localFolder storage address for the file ex. Radiology\X-Ray
      //var localFolder: string = "PatientFiles\\" + patReport.FileType;
      var fileName: string;
      //patient object was included to display it's details on client side
      //it is not necessary during post
      var omited = _.omit(patReport, ['PatientFilesValidator']);
      //we've to encode uri since we might have special characters like , / ? : @ & = + $ #  etc in our report value.
      var reportDetails = JSON.stringify(omited);//encodeURIComponent();
      let uploadedImgCount = 0;
      //ImageName can contain names of more than one image seperated by ;
      //if (patReport.ImageName)
      //    uploadedImgCount = patReport.ImageName.split(";").length;
      for (var i = 0; i < filesToUpload.length; i++) {
        //to get the imagetype
        let splitImagetype = filesToUpload[i].name.split(".");
        let imageExtension = splitImagetype[1];
        //fileName = PatientId_ImagingItemName_PatientVisitId_CurrentDateTime_Counter.imageExtension
        fileName = patReport.FileType + "_" + moment().format('DD-MM-YY') + "." + imageExtension;
        formToPost.append("uploads", filesToUpload[i], fileName);
      }
      //pending reports has ImagingReportId
      //new reports does not has ImagingReportId
      //if ImagingReportId is present then update item.
      formToPost.append("reportDetails", reportDetails);

      //let finalIPResult = input;
      return this.patientDLService.PostPatientFiles(formToPost)
        .map(res => res);

    } catch (exception) {
      throw exception;
    }
  } public GetMunicipality(id: number) {
    return this.patientDLService.GetMunicipality(id)
      .map(res => { return res })
  }
  public GetFileFromServer(id: number) {
    return this.patientDLService.GetFileFromServer(id).map((res) => {
      return res;
    });
  }

  public GetPatientCurrentSchemeMap(patientId: number, patientVisitId: number) {
    return this.patientDLService.GetPatientCurrentSchemeMap(patientId, patientVisitId).map((res) => {
      return res;
    })
  }
  public GetPatientDashboardCardSummaryCalculation(FromDate, ToDate) {
    try {
      return this.patientDLService.GetPatientDashboardCardSummaryCalculation(FromDate, ToDate)
        .map(res => { return res });
    }
    catch (ex) {
      throw ex;
    }
  }
  public GetPatientCountByDay(FromDate, ToDate) {
    try {
      return this.patientDLService.GetPatientCountByDay(FromDate, ToDate)
        .map(res => { return res });
    }
    catch (ex) {
      throw ex;
    }
  }
  public GetAverageTreatmentCostbyAgeGroup(FromDate, ToDate) {
    try {
      return this.patientDLService.GetAverageTreatmentCostbyAgeGroup(FromDate, ToDate)
        .map(res => { return res });
    }
    catch (ex) {
      throw ex;
    }
  }
  public GetDepartmentWiseAppointment(FromDate, ToDate) {
    try {
      return this.patientDLService.GetDepartmentWiseAppointment(FromDate, ToDate)
        .map(res => { return res });
    }
    catch (ex) {
      throw ex;
    }

  }
  public GetPAtVisitByMembership(FromDate, ToDate) {
    try {
      return this.patientDLService.GetPAtVisitByMembership(FromDate, ToDate)
        .map(res => { return res });
    }
    catch (ex) {
      throw ex;
    }

  }
  public GetPatientDistributionBasedOnRank(FromDate, ToDate, DepartmentId: number) {
    try {
      return this.patientDLService.GetPatientDistributionBasedOnRank(FromDate, ToDate, DepartmentId)
        .map(res => { return res });
    }
    catch (ex) {
      throw ex;
    }

  }
  public GetHospitalManagement(FromDate, ToDate) {
    try {
      return this.patientDLService.GetHospitalManagement(FromDate, ToDate)
        .map(res => { return res });
    }
    catch (ex) {
      throw ex;
    }
  }
  public GetDepartment() {
    try {
      return this.patientDLService.GetDepartment()
        .map(res => { return res });
    }
    catch (ex) {
      throw ex;
    }
  }


  public GetCastEthnicGroupList() {
    return this.patientDLService.GetCastEthnicGroupList().map((res) => {
      return res;
    });
  }

  GetPatientHealthCardTemplates(templateCode: string, patientid: number) {
    return this.patientDLService.GetPatientHealthCardTemplates(templateCode, patientid).map(res => { return res; });
  }
}

