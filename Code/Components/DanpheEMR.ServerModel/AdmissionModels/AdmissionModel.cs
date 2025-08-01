﻿using DanpheEMR.ServerModel.PatientModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanpheEMR.ServerModel
{
    public class AdmissionModel
    {
        [DatabaseGeneratedAttribute(DatabaseGeneratedOption.Identity)]
        public int PatientAdmissionId { get; set; }

        [Key, ForeignKey("Visit")]
        public int PatientVisitId { get; set; }
        public int PatientId { get; set; }
        public int? AdmittingDoctorId { get; set; }
        public DateTime AdmissionDate { get; set; }
        public DateTime? DischargeDate { get; set; }
        public string AdmissionNotes { get; set; }
        public string AdmissionOrders { get; set; }
        public string AdmissionStatus { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public virtual VisitModel Visit { get; set; }
        //public DateTime? TransferDate { get; set; }
        public string BillStatusOnDischarge { get; set; }
        public string DischargeRemarks { get; set; }
        public int? DischargedBy { get; set; }
        public int? ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }

        public string CareOfPersonName { get; set; }
        public string CareOfPersonPhoneNo { get; set; }
        public string CareOfPersonRelation { get; set; }
        public virtual List<PatientBedInfo> PatientBedInfos { get; set; }
        [NotMapped]
        public virtual BillingDepositModel BilDeposit { get; set; }
        [NotMapped]
        public BillingTransactionModel BillingTransaction { get; set; }

        //RequestingDepartmentId is moved to ADT_TXN_PatientBedInfo table.
        [NotMapped]
        public int? RequestingDeptId { get; set; }//sud:19Jun'18

        //Added by Yubraj --19th November 2018
        public DateTime? CancelledOn { get; set; }
        public int? CancelledBy { get; set; }
        public string CancelledRemark { get; set; }

        public string ProcedureType { get; set; }

        public bool IsPoliceCase { get; set; }
        [NotMapped]
        public bool Ins_HasInsurance { get; set; }
        [NotMapped]
        //sud:1-Oct'21--Changing Claimcode from String to Int64-- to use Incremental logic (max+1)
        //need nullable since ClaimCode is Non-Mandatory for normal visits.
        public Int64? ClaimCode { get; set; }
        [NotMapped]
        public string Ins_NshiNumber { get; set; }

        public bool IsInsurancePatient { get; set; }
        [NotMapped]
        public double? Ins_InsuranceBalance { get; set; }
        public int? DiscountSchemeId { get; set; }
        public string AdmissionCase { get; set; }
        [NotMapped]
        public bool IsBillingEnabled { get; set; }

        [NotMapped]
        public bool IsLastClaimCodeUsed { get; set; }//sud:1-Oct'21

        public double ProvisionalDiscPercent { get; set; }
        public bool IsItemDiscountEnabled { get; set; }
        [NotMapped]
        public int PriceCategoryId { get; set; }
        [NotMapped]
        public virtual PatientSchemeMapModel PatientSchemesMap { get; set; }
        [NotMapped]
        public CareofPerson_DTO CareTaker { get; set; }
        public bool IsProvisionalDischarge { get; set; }//Krishna, 29thAug'23, This will track Provisional Discharge
        public bool IsProvisionalDischargeCleared { get; set; }//Krishna, 29thAug'23, This will track Whether Provisional Discharge is cleared or not.

        [NotMapped]
        public List<AddPatientVisitConsultants_DTO> AddPatientVisitConsultants { get; set; }
      


    }
    //Bibek, 26thJune'23 
   public class CareofPerson_DTO
    {
        public string CareOfPersonName { get; set; }
        public string CareOfPersonRelation { get; set; }
        public string CareOfPersonPhoneNo { get; set; }
    }

    //Krishna, 09thApril, 2024, This is added here because we need to pass Consultants in the Body of Admission Request which is using AdmissionModel and we could not make reference to the project where DTO is kept
    public class AddPatientVisitConsultants_DTO
    {
        public string VisitType { get; set; }
        public int ConsultantId { get; set; }
        public bool IsPrimaryConsultant { get; set; }
    }
}
