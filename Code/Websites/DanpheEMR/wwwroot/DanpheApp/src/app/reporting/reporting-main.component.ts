import { Component } from '@angular/core';
import { SecurityService } from "../security/shared/security.service";
import { ReportingService } from './shared/reporting-service';


@Component({
  templateUrl: "./reporting-main.html"
})

export class RPT_ReportingMainComponent {
  validRoutes: any;
  public primaryNavItems: Array<any> = null;
  public secondaryNavItems: Array<any> = null;
  constructor(public securityService: SecurityService,
    private _reportingService: ReportingService) {
    //get the chld routes of Reports from valid routes available for this user.
    this.validRoutes = this.securityService.GetChildRoutes("Reports");
    this.primaryNavItems = this.validRoutes.filter(a => a.IsSecondaryNavInDropdown == null || a.IsSecondaryNavInDropdown == 0);
    this.secondaryNavItems = this.validRoutes.filter(a => a.IsSecondaryNavInDropdown == 1);
    this._reportingService.GetSchemesForReport();
  }
}
