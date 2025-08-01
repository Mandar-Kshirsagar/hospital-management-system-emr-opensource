import {
    FormBuilder,
    FormGroup,
    Validators
} from '@angular/forms';

export class PHRMGenericModel {
    public GenericId: number = 0;
    public GenericName: string = '';
    public GeneralCategory: string = '';
    public TherapeuticCategory: string = '';
    public Counseling: string = '';
    public CreatedBy: number = 0;
    public CreatedOn: string = '';
    public ModifiedBy: number = 0;
    public ModifiedOn: string = '';
    public IsActive: boolean = true;
    public CategoryId: number = 0;
    public ItemCode: string = '';

    //sud: 13July'18--these are not in Generic table, we're joining in server side to get these values. 
    //public Dosage: string = null;
    //public Frequency: string = null;
    //public FrequencyDescription: string = null;
    //public Duration: string = null;

    public GenericValidator: FormGroup = null;


    constructor() {
        var _formBuilder = new FormBuilder();
        this.GenericValidator = _formBuilder.group({
            'GenericName': ['', Validators.compose([Validators.required])],
            'CategoryId': ['', Validators.compose([Validators.required])]
        });
    }

    public IsDirty(fieldname): boolean {
        if (fieldname == undefined) {
            return this.GenericValidator.dirty;
        }
        else {
            return this.GenericValidator.controls[fieldname].dirty;
        }

    }

    public IsValid(): boolean { if (this.GenericValidator.valid) { return true; } else { return false; } } public IsValidCheck(fieldname, validator): boolean {
        if (fieldname == undefined) {
            return this.GenericValidator.valid;
        }
        else {
            return !(this.GenericValidator.hasError(validator, fieldname));
        }
    }



}
