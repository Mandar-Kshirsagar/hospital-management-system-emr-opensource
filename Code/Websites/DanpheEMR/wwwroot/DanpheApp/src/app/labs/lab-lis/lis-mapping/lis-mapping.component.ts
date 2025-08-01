import {
    ChangeDetectorRef,
    Component
} from '@angular/core';
import { CoreService } from '../../../core/shared/core.service';
import { SecurityService } from '../../../security/shared/security.service';
import { GridEmitModel } from '../../../shared/danphe-grid/grid-emit.model';
import { MessageboxService } from '../../../shared/messagebox/messagebox.service';
import { ENUM_DanpheHTTPResponseText, ENUM_MessageBox_Status } from '../../../shared/shared-enums';
import LabLISGridColumnSettings from '../shared/lis-grid-col.settings';
import { LabLISBLService } from '../shared/lis.bl.service';

@Component({
    templateUrl: "./lis-mapping.html"
})

export class LISMappingComponent {
    public allMappedData: Array<any>;
    public lisMappingGridCols: Array<any>;
    public showMappingAdd: boolean = false;
    public showMappingDeleteAlert: boolean = false;
    public selectedLisCompMapId: number;


    constructor(public coreService: CoreService, public securityService: SecurityService, public labLISBlService: LabLISBLService,
        public changeDetector: ChangeDetectorRef, public messageService: MessageboxService) {
        this.GetAllMappedDataData();
        this.lisMappingGridCols = LabLISGridColumnSettings.ComponentMappingListCols;
    }

    public GetAllMappedDataData() {
        this.coreService.loading = true;
        this.labLISBlService.GetAllLISMappedData().subscribe(res => {
            if (res.Status === ENUM_DanpheHTTPResponseText.OK) {
                this.allMappedData = res.Results;
                this.coreService.loading = false;
            }
        }, (err) => {
            this.messageService.showMessage(ENUM_MessageBox_Status.Notice, ["Unable to get LIS Mappted data."]);
            this.coreService.loading = false;
        });
    }

    GridActions(event: GridEmitModel) {
        switch (event.Action) {
            case "edit": {
                this.selectedLisCompMapId = event.Data.LISComponentMapId;
                this.showMappingAdd = false;
                this.changeDetector.detectChanges();
                this.showMappingAdd = true;
                break;
            }
            case "delete": {
                this.selectedLisCompMapId = event.Data.LISComponentMapId;
                this.showMappingDeleteAlert = false;
                this.changeDetector.detectChanges();
                this.showMappingDeleteAlert = true;
                break;
            }
            case "activate": {
                this.ActivateMapping(event.Data.LISComponentMapId)
                break;
            }
            default:
                break;
        }
    }

    AddNewMapping() {
        this.showMappingAdd = true;
    }

    Remove(decision: boolean) {
        if (decision) {
            this.labLISBlService.RemoveLisMapping(this.selectedLisCompMapId).subscribe(res => {
                if (res.Status === ENUM_DanpheHTTPResponseText.OK) {
                    this.showMappingDeleteAlert = false;
                    this.selectedLisCompMapId = null;
                    this.GetAllMappedDataData();
                }
            }, (err) => {
                console.log(err.error.ErrorMessage); this.coreService.loading = false; this.selectedLisCompMapId = null;
                this.messageService.showMessage(ENUM_MessageBox_Status.Error, ['Sorry this mapping cannot be deleted now. Please try again Later.'])
            });
        } else {
            this.selectedLisCompMapId = null;
            this.showMappingDeleteAlert = false;
        }
    }

    CloseAddMappingPopUp($event) {
        this.showMappingAdd = false;
        if ($event && $event.dataUpdated) {
            this.GetAllMappedDataData();
        }
        this.selectedLisCompMapId = null;
    }

    ActivateMapping(id: number) {
        this.coreService.loading = true;
        this.labLISBlService.ActivateMapping(id)
            .finally(() => { this.coreService.loading = false; })
            .subscribe(res => {
                if (res.Status === ENUM_DanpheHTTPResponseText.OK) {
                    this.GetAllMappedDataData();
                }
            }, (err) => {
                console.log(err.error.ErrorMessage);
                this.messageService.showMessage(ENUM_MessageBox_Status.Error, ['Sorry this mapping cannot be activated now. Please try again Later.'])
            });
    }
}
