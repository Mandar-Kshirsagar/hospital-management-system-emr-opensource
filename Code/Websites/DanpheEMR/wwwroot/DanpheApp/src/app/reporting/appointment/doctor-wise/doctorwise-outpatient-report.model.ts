import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import * as moment from 'moment/moment';

export class RPT_APPT_DoctorWiseOutPatientReportModel {
    public fromDate: string = "";
    public toDate: string = "";
    SchemeId: number = null;
    public DoctorwiseOutPatientReportValidator: FormGroup = null;

    constructor() {
        var _formBuilder = new FormBuilder();
        this.DoctorwiseOutPatientReportValidator = _formBuilder.group({
            'fromDate': ['', Validators.compose([Validators.required, this.dateValidatorsForPast])],
            'toDate': ['', Validators.compose([Validators.required, this.dateValidator])]
        });
    }

    dateValidator(control: FormControl): { [key: string]: boolean } {
        var currDate = moment().format('YYYY-MM-DD HH:mm');
        if (control.value) { // gets empty string for invalid date such as 30th Feb or 31st Nov)
            if ((moment(control.value).diff(currDate) > 0)
                || (moment(currDate).diff(control.value, 'years') > 200)) //can select date upto 200 year past from today.
                return { 'wrongDate': true };
        }
        else
            return { 'wrongDate': true };
    }

    dateValidatorsForPast(control: FormControl): { [key: string]: boolean } {
        //get current date, month and time
        var currDate = moment().format('YYYY-MM-DD');
        if (control.value) {
            //if positive then selected date is of future else it of the past
            if ((moment(control.value).diff(currDate) > 0) ||
                (moment(control.value).diff(currDate, 'years') < -200)) // this will not allow the age diff more than 200 is past
                return { 'wrongDate': true };
        }
        else
            return { 'wrongDate': true };
    }


    public IsDirty(fieldName): boolean {
        if (fieldName == undefined)
            return this.DoctorwiseOutPatientReportValidator.dirty;
        else
            return this.DoctorwiseOutPatientReportValidator.controls[fieldName].dirty;
    }

    public IsValid(): boolean { if (this.DoctorwiseOutPatientReportValidator.valid) { return true; } else { return false; } } public IsValidCheck(fieldName, validator): boolean {
        if (fieldName == undefined)
            return this.DoctorwiseOutPatientReportValidator.valid;
        else
            return !(this.DoctorwiseOutPatientReportValidator.hasError(validator, fieldName));
    }
}
