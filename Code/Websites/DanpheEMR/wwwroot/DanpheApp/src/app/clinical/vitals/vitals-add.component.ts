// <reference path="../../radiology/imaging/imaging-requisition-list.component.ts" />
import { ChangeDetectorRef, Component, EventEmitter, Input, Output, Renderer2 } from "@angular/core";

import { VisitService } from '../../appointments/shared/visit.service';
import { MessageboxService } from '../../shared/messagebox/messagebox.service';
import { IOAllergyVitalsBLService } from '../shared/io-allergy-vitals.bl.service';

import { PatientService } from "../../patients/shared/patient.service";
import { ENUM_MessageBox_Status, ENUM_VitalVerbalScale, ENUM_VitalsEyeScale, ENUM_VitalsMotorScale } from "../../shared/shared-enums";
import { Vitals } from "../shared/vitals.model";

@Component({
  selector: "vitals-add",
  templateUrl: "./vitals-add.html"
})
export class VitalsAddComponent {
  public showAddVitalBox: boolean = false;
  public CurrentVital: Vitals = new Vitals();

  @Input("selectedVitals")
  public selectedVitals: any;

  @Input("visitIdfromADT")
  public visitIdfromADT: any;

  @Input("showVitalList") showVitalList: boolean = false;

  @Output("callBackShowHide")
  callBackDisplay: EventEmitter<object> = new EventEmitter<object>();

  @Output("callback-add")
  callbackAdd: EventEmitter<Object> = new EventEmitter<Object>();

  @Output("notify-adt")
  notifyAdt: EventEmitter<boolean> = new EventEmitter<boolean>();

  public vitalsList: Array<Vitals> = new Array<Vitals>();
  public painData: Array<{ BodyPart: "", PainScale: 0 }> = [];

  public foot: number = null;
  public inch: number = null;
  //if footInch is selected then 2 input box is displayed.
  public footInchSelected: boolean = false;
  public updateButton: boolean = false;
  public selectedIndex: number = null;
  public loading: boolean = false;
  public painDataList: Array<any> = new Array<any>();

  //@Input("showVitalAddBox")
  //public set showVitalAddBox(val: boolean) {
  //  this.showAddVitalBox = val;
  //  if (this.selectedVitals) {
  //    this.updateButton = true;
  //    if (this.selectedVitals.HeightUnit == "inch") {
  //      let split = this.selectedVitals.Height.split("'");
  //      this.foot = Number(split[0]);
  //      this.inch = Number(split[1]);
  //      this.footInchSelected = true;
  //    }

  //    this.painData = [];

  //    if (this.selectedVitals.BodyPart) {
  //      var jsonData = JSON.parse(this.selectedVitals.BodyPart);
  //      jsonData.forEach(val => {
  //        this.painData.push(val);
  //      });
  //    } else {
  //      this.painData.push({ BodyPart: "", PainScale: null });
  //    }

  //    this.CurrentVital = Object.assign(this.CurrentVital, this.selectedVitals);
  //  }
  //  else {
  //    this.CurrentVital = new Vitals();
  //    this.painData = [];
  //    this.painData.push({ BodyPart: "", PainScale: null });
  //    this.CurrentVital.OxygenDeliveryMethod = 'Room Air';
  //    this.updateButton = false;
  //  }
  //}




  constructor(public visitService: VisitService,
    public ioAllergyVitalsBLService: IOAllergyVitalsBLService,
    public changeDetector: ChangeDetectorRef, public msgBoxServ: MessageboxService,
    public patientService: PatientService,
    public renderer2: Renderer2) {
    this.painData.push({ BodyPart: "", PainScale: null });
  }

  ngOnInit() {

    if (this.selectedVitals) {
      this.updateButton = true;
      if (this.selectedVitals.HeightUnit == "inch") {
        let split = this.selectedVitals.Height.split("'");
        this.foot = Number(split[0]);
        this.inch = Number(split[1]);
        this.footInchSelected = true;
      }

      this.painData = [];

      if (this.selectedVitals.BodyPart) {
        var jsonData = JSON.parse(this.selectedVitals.BodyPart);
        jsonData.forEach(val => {
          this.painData.push(val);
        });
      } else {
        this.painData.push({ BodyPart: "", PainScale: null });
      }
      this.changeDetector.detectChanges();
      this.CurrentVital = Object.assign(this.CurrentVital, this.selectedVitals);
    }
    else {
      this.CurrentVital = new Vitals();
      this.painData = [];
      this.painData.push({ BodyPart: "", PainScale: null });
      this.CurrentVital.OxygenDeliveryMethod = null;
      this.updateButton = false;
    }

    this.GetPatientVitalsList();
  }

  //gets the list of vitals of the selected patient.
  GetPatientVitalsList(): void {
    let patientVisitId = this.visitService.getGlobal().PatientVisitId;

    if (!patientVisitId) {
      patientVisitId = this.visitIdfromADT;
    }

    this.ioAllergyVitalsBLService.GetPatientVitalsList(patientVisitId)
      .subscribe(res => {
        if (res.Status == "OK") {
          this.CallBackGetPatientVitalList(res.Results);
        }
        else {
          this.msgBoxServ.showMessage("failed", ['Failed. please check log for details.'], res.ErrorMessage);

        }
      },
        err => { this.msgBoxServ.showMessage("error", [err.ErrorMessage]); });
  }
  //call back funtion for get patient vitals
  CallBackGetPatientVitalList(_vitalsList) {
    //clear Old painDataList to remove duplication
    this.painDataList = [];
    //looping through the vitalsList to check if any object contains height unit as inch so that it can be converted to foot inch.
    for (var i = 0; i < _vitalsList.length; i++) {
      if (_vitalsList[i].HeightUnit && _vitalsList[i].HeightUnit == "inch") {
        //incase of footinch we're converting and storing as inch.
        //converting back for displaying in the format foot'inch''
        _vitalsList[i].Height = this.ioAllergyVitalsBLService.ConvertInchToFootInch(_vitalsList[i].Height);
      }
      let jsonData = JSON.parse(_vitalsList[i].BodyPart);
      this.painDataList.push(jsonData);
    }
    this.vitalsList = _vitalsList;
    this.MapVitalData();

    // this.callbackAdd.emit({ vitals: this.vitalsList, submit: false });
  }

  //enables the update button and assigns the selected vitals object to the CurrentVital object.
  public Edit(selectedVitals, selIndex: number) {
    this.selectedVitals = null;
    this.selectedIndex = selIndex;

    this.changeDetector.detectChanges();
    this.selectedVitals = selectedVitals;

    if (!this.selectedVitals.TemperatureUnit) {
      this.selectedVitals.TemperatureUnit = "F";
    }
    if (!this.selectedVitals.HeightUnit) {
      this.selectedVitals.HeightUnit = "cm";
    }
    if (!this.selectedVitals.WeightUnit) {
      this.selectedVitals.WeightUnit = "kg";
    }

    this.updateButton = true;

    if (this.selectedVitals.HeightUnit == "inch") {
      let split = this.selectedVitals.Height.split("'");
      this.foot = Number(split[0]);
      this.inch = Number(split[1]);
      this.CurrentVital.Height = null; // Clear cm height
      this.footInchSelected = true;
    } else if (this.selectedVitals.HeightUnit === "cm") {
      // Handle case where height is in cm
      this.CurrentVital.Height = Number(this.selectedVitals.Height);
      this.foot = null; // Clear feet and inches values
      this.inch = null;
      this.footInchSelected = false; // Hide foot and inch fields
    }

    this.painData = [];

    if (this.selectedVitals.BodyPart) {
      var jsonData = JSON.parse(this.selectedVitals.BodyPart);
      jsonData.forEach(val => {
        this.painData.push(val);
      });
    } else {
      this.painData.push({ BodyPart: "", PainScale: null });
    }

    this.CurrentVital = Object.assign(this.CurrentVital, this.selectedVitals);


  }

  HeightUnitChanged(): void {
    //resetting the input details and changing the display of heightinput fields accordingly.
    if (this.CurrentVital.HeightUnit == "inch") {//incase of footinch we're converting and storing as inch.
      this.CurrentVital.Height = null;
      this.CurrentVital.BMI = null;
      this.footInchSelected = true;
    }
    else {
      this.changeDetector.detectChanges();
      this.CurrentVital.BMI = null;
      this.foot = null;
      this.inch = null;
      this.CurrentVital.Height = null;
      this.footInchSelected = false;
    }
  }


  CalculateBMI(): void {
    if (this.foot || this.inch) {
      let _foot = this.foot ? this.foot : 0;
      let _inch = this.inch ? this.inch : 0;
      //incase of footinch we're converting and storing as inch.
      this.CurrentVital.Height = (_foot * 12) + _inch;
    }
    //if both height and weight is entered we're calculating the BMI.
    if (this.CurrentVital.Height && this.CurrentVital.Weight) {
      this.CurrentVital.BMI = this.ioAllergyVitalsBLService.
        CalculateBMI(this.CurrentVital.Height,
          this.CurrentVital.Weight,
          this.CurrentVital.HeightUnit,
          this.CurrentVital.WeightUnit);
    }
  }

  //post new vitals
  AddVitals(): void {
    let flag = 1;
    if (!this.loading) {
      if (this.CurrentVital) {
        if (this.CurrentVital.Height < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["Height cannot be less than 0"]);
          return;
        }
        if (this.CurrentVital.Weight < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["Weight cannot be less than 0"]);
          return;
        }
        if (this.CurrentVital.Temperature < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["Temperature cannot be less than 0"]);
          return;
        }
        if (this.CurrentVital.Pulse < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["Pulse rate cannot be less than 0"]);
          return;
        }
        if (this.CurrentVital.SpO2 < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["SpO2 cannot be less than 0"]);
          return;
        }
        if (parseInt(this.CurrentVital.RespiratoryRatePerMin) < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["Please provide proper respiratory rate"]);
          return;
        }
      }
      if (this.painData) {
        for (var i = 0; i < this.painData.length; i++) {
          if ((this.painData[i].BodyPart && !this.painData[i].PainScale) || (!this.painData[i].BodyPart && this.painData[i].PainScale)) {
            this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["Please Enter Body Pain Data Properly"]);
            flag = 0;
            break;
          } else if (!this.painData[i].BodyPart && !this.painData[i].PainScale && i > 0) {
            this.painData.splice(i, 1);
            i--;
          }
        }
      }
      else {
        this.painData = [];
        this.painData.push({ BodyPart: "", PainScale: null });
      }
      //Either enter both BodyPart and pain rate or Don't enter anything on it


      //atleast one vital should be entered.
      if ((this.CurrentVital.BMI || this.CurrentVital.Height || this.CurrentVital.Weight
        || this.CurrentVital.BPDiastolic || this.CurrentVital.BPSystolic
        || this.CurrentVital.OxygenDeliveryMethod || this.CurrentVital.Pulse || this.CurrentVital.RespiratoryRatePerMin
        || this.CurrentVital.SpO2 || this.CurrentVital.Temperature) && flag) {
        if (this.CurrentVital.IsValidCheck() && this.CurrentVital.IsBPComplete()) {
          this.CurrentVital.PatientVisitId = this.visitService.getGlobal().PatientVisitId;
          //only for the ones sent from ADT
          if (this.visitIdfromADT) {
            this.CurrentVital.PatientVisitId = this.visitIdfromADT;
          }
          this.loading = true;
          var jsonData = JSON.stringify(this.painData);
          this.CurrentVital.BodyPart = jsonData;
          this.ioAllergyVitalsBLService.PostVitals(this.CurrentVital)
            .subscribe(res => this.CallBackAddVitals(res),
              err => { this.msgBoxServ.showMessage("error", [err]); });
        }
      }
      else {
        this.msgBoxServ.showMessage("error", ["Enter atleast one Vital information"]);

      }
    }

  }
  //call back of add vitals.
  CallBackAddVitals(res) {
    this.loading = false;
    if (res.Status == "OK") {
      //var jsonData = JSON.parse(res.Results.BodyPart);
      //this.painDataList.push(jsonData);

      //this.painData = [];
      //this.painData.push({ BodyPart: "", PainScale: null });

      //if (res.Results.HeightUnit == "inch") {
      //    //incase of footinch we're converting and storing as inch.
      //    //converting back for displaying in the format foot'inch''
      //    res.Results.Height = this.ioAllergyVitalsBLService.ConvertInchToFootInch(res.Results.Height);
      //}






      //this.vitalsList.push(res.Results);
      this.callBackDisplay.emit({ vitals: res.Results });
      this.callbackAdd.emit({ vitals: res.Results, submit: true });
      this.notifyAdt.emit(false);
      this.painData = [];
      this.painData.push({ BodyPart: "", PainScale: null });

      this.msgBoxServ.showMessage("Success", ['Your Vital is added']);
      this.CurrentVital = new Vitals();
      this.foot = null;
      this.inch = null;
      this.footInchSelected = false;
      this.closeAddBox();
    }
    else {
      this.msgBoxServ.showMessage("failed", ['Failed. please check log for details.'], res.ErrorMessage);

    }
  }



  public Update() {
    let flag = 1;

    if (!this.loading) {
      if (this.CurrentVital) {
        if (this.CurrentVital.Height < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["Height cannot be less than 0"]);
          return;
        }
        if (this.CurrentVital.Weight < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["Weight cannot be less than 0"]);
          return;
        }
        if (this.CurrentVital.Temperature < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["Temperature cannot be less than 0"]);
          return;
        }
        if (this.CurrentVital.Pulse < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["Pulse rate cannot be less than 0"]);
          return;
        }
        if (this.CurrentVital.SpO2 < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["SpO2 cannot be less than 0"]);
          return;
        }
        if (this.CurrentVital.BPDiastolic < 0 || this.CurrentVital.BPSystolic < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["BP cannot be less than 0"]);
          return;
        }
        if (parseInt(this.CurrentVital.RespiratoryRatePerMin) < 0) {
          this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Error, ["Please provide proper respiratory rate"]);
          return;
        }
      }
      if (this.painData) {
        for (var j = 0; j < this.painData.length; j++) {
          if ((this.painData[j].BodyPart && !this.painData[j].PainScale) || (!this.painData[j].BodyPart && this.painData[j].PainScale)) {
            this.msgBoxServ.showMessage("error", ["Please Enter Body Pain Data Properly"]);
            flag = 0;
            break;
          } else if (!this.painData[j].BodyPart && !this.painData[j].PainScale && j > 0) {
            this.painData.splice(j, 1);
            j--;
          }
        }
      }
      else {
        this.painData = [];
        this.painData.push({ BodyPart: "", PainScale: null });
      }

      //marking every fields as dirty and checking validity
      for (var i in this.CurrentVital.VitalsValidator.controls) {
        this.CurrentVital.VitalsValidator.controls[i].markAsDirty();
        this.CurrentVital.VitalsValidator.controls[i].updateValueAndValidity();
      }
      //atleast one vital should be entered.
      if ((this.CurrentVital.BMI || this.CurrentVital.BodyPart
        || this.CurrentVital.Height || this.CurrentVital.Weight
        || this.CurrentVital.BPDiastolic || this.CurrentVital.BPSystolic
        || this.CurrentVital.OxygenDeliveryMethod || this.CurrentVital.PainScale
        || this.CurrentVital.Pulse || this.CurrentVital.RespiratoryRatePerMin
        || this.CurrentVital.SpO2 || this.CurrentVital.Temperature) && flag) {
        if (this.CurrentVital.IsValidCheck() && this.CurrentVital.IsBPComplete()) {
          if (this.CurrentVital.HeightUnit == "inch" && this.foot || this.inch)
            //incase of footinch we're converting and storing as inch.
            this.CurrentVital.Height = (this.foot * 12) + this.inch;
          this.loading = true;
          var jsonData = JSON.stringify(this.painData);

          if (this.painData) {
            this.painDataList.splice(this.selectedIndex, 1);
            this.painDataList.splice(this.selectedIndex, 0, this.painData);
            this.painDataList.slice();
          }

          this.CurrentVital.BodyPart = jsonData;
          this.ioAllergyVitalsBLService.PutVitals(this.CurrentVital)
            .subscribe(
              res => {
                this.CallBackUpdateVitals(res);
              });
        }
      }
      else
        this.msgBoxServ.showMessage("error", ["Enter atleast one Vital information"]);
    }


  }
  MapVitalData() {
    this.vitalsList.forEach((vitals) => {
      if (vitals.Eyes) {
        vitals.Eyes = this.MapFetchedEyeScale(vitals.Eyes);
      }
      if (vitals.Motor) {
        vitals.Motor = this.MapFetchedMotorScale(vitals.Motor);
      }
      if (vitals.Verbal) {
        vitals.Verbal = this.MapFetchedVerbalScale(vitals.Verbal);
      }
    });
  }
  MapFetchedEyeScale(value: string): string {
    switch (value) {
      case ENUM_VitalsEyeScale.Scale1:
        return "1";
      case ENUM_VitalsEyeScale.Scale2:
        return "2";
      case ENUM_VitalsEyeScale.Scale3:
        return "3";
      case ENUM_VitalsEyeScale.Scale4:
        return "4";
      default:
    }
  }
  MapFetchedMotorScale(value: string): string {
    switch (value) {
      case ENUM_VitalsMotorScale.Scale1:
        return "1";
      case ENUM_VitalsMotorScale.Scale2:
        return "2";
      case ENUM_VitalsMotorScale.Scale3:
        return "3";
      case ENUM_VitalsMotorScale.Scale4:
        return "4";
      case ENUM_VitalsMotorScale.Scale5:
        return "5";
      case ENUM_VitalsMotorScale.Scale6:
        return "6";
      default:
    }
  }
  MapFetchedVerbalScale(value: string): string {
    switch (value) {
      case ENUM_VitalVerbalScale.Scale1:
        return "1";
      case ENUM_VitalVerbalScale.Scale2:
        return "2";
      case ENUM_VitalVerbalScale.Scale3:
        return "3";
      case ENUM_VitalVerbalScale.Scale4:
        return "4";
      case ENUM_VitalVerbalScale.Scale5:
        return "5";
      default:
    }
  }
  //call back function for put vitals.
  CallBackUpdateVitals(res) {
    this.loading = false;
    if (res.Status == "OK") {
      if (this.showVitalList) {
        // this.GetPatientVitalsList();
        this.MapVitalData();
        this.changeDetector.detectChanges();
        this.painData = [];
        this.painData.push({ BodyPart: "", PainScale: null });
        this.callbackAdd.emit({ vitals: res.Results, submit: true });
        this.CurrentVital = new Vitals();
        // this.foot = null;
        // this.inch = null;
        this.footInchSelected = false;
        this.updateButton = false;
      } else {
        this.callbackAdd.emit({ vitals: res.Results, submit: true });

        this.painData = [];
        this.painData.push({ BodyPart: "", PainScale: null });
        this.CurrentVital = new Vitals();
        this.footInchSelected = false;
        this.updateButton = false;
        this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Success, ["updated successfully"]);
        // const vitalList = res.Results;
        // this.vitalsList[this.selectedIndex] =vitalList;
        this.MapVitalData();
      }
      this.foot = null;
      this.inch = null;
    }
    else {
      this.msgBoxServ.showMessage(ENUM_MessageBox_Status.Failed, ['Failed. please check log for details.'], res.ErrorMessage);

    }
  }

  AddMorePainData() {
    this.painData.push({ BodyPart: "", PainScale: null });
  }

  DeletePainData(indx) {
    this.painData.splice(indx, 1);
  }

  closeAddBox() {
    //this.showVitalAddBox = false;
    //let vt = new Vitals();
    this.callBackDisplay.emit({ vitals: null });
    this.callbackAdd.emit({ vitals: null, submit: false });
    //this.callbackAdd.emit({});
    if (this.visitIdfromADT) { this.notifyAdt.emit(false); }
  }

  GoToNext(nextField: HTMLInputElement) {
    nextField.focus();
    nextField.select();
  }

  GoToNextSelect(nextField: HTMLSelectElement) {
    nextField.focus();
  }

  change() {
    let onFieldChange = this.renderer2.selectRootElement('#bodyPart');
    onFieldChange.focus();
  }
  MapEyeScale(value: number): string {
    switch (value) {
      case 1:
        return ENUM_VitalsEyeScale.Scale1;
      case 2:
        return ENUM_VitalsEyeScale.Scale2;
      case 3:
        return ENUM_VitalsEyeScale.Scale3;
      case 4:
        return ENUM_VitalsEyeScale.Scale4;
      default:
    }
  }
  MapMotorScale(value: number): string {
    switch (value) {
      case 1:
        return ENUM_VitalsMotorScale.Scale1;
      case 2:
        return ENUM_VitalsMotorScale.Scale2;
      case 3:
        return ENUM_VitalsMotorScale.Scale3;
      case 4:
        return ENUM_VitalsMotorScale.Scale4;
      case 5:
        return ENUM_VitalsMotorScale.Scale5;
      case 6:
        return ENUM_VitalsMotorScale.Scale6;
      default:
    }
  }
  MapVerbalScale(value: number): string {
    switch (value) {
      case 1:
        return ENUM_VitalVerbalScale.Scale1;
      case 2:
        return ENUM_VitalVerbalScale.Scale2;
      case 3:
        return ENUM_VitalVerbalScale.Scale3;
      case 4:
        return ENUM_VitalVerbalScale.Scale4;
      case 5:
        return ENUM_VitalVerbalScale.Scale5;
      default:
    }
  }
  onEyeModelChange(value: string): void {
    const selectedValue = parseInt(value, 10);
    if (isNaN(selectedValue)) {
      console.error("Invalid input: Unable to convert to a number.");
      return;
    }
    this.CurrentVital.Eyes = this.MapEyeScale(selectedValue);
  }
  onMotorModelChange(value: string): void {
    const selectedValue = parseInt(value, 10);
    if (isNaN(selectedValue)) {
      console.error("Invalid input: Unable to convert to a number.");
      return;
    }
    this.CurrentVital.Motor = this.MapMotorScale(selectedValue);
  }
  onVerbalModelChange(value: string): void {
    const selectedValue = parseInt(value, 10);
    if (isNaN(selectedValue)) {
      console.error("Invalid input: Unable to convert to a number.");
      return;
    }
    this.CurrentVital.Verbal = this.MapVerbalScale(selectedValue);
  }

}

