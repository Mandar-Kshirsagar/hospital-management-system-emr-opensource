﻿import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { HelpDeskDLService } from './helpdesk.dl.service';
@Injectable()
export class HelpDeskBLService {
  constructor(public router: Router, public helpdeskDLService: HelpDeskDLService) {
  }
  public LoadBedInfo() {
    return this.helpdeskDLService.GetBedinfo()
      .map(res => res);
  }
  public LoadEmployeeInfo() {
    return this.helpdeskDLService.GetEmployeeinfo()
      .map(res => res);
  }
  public LoadWardInfo() {
    return this.helpdeskDLService.GetWardinfo()
      .map(res => res);
  }



  LoadBedPatientInfo() {
    return this.helpdeskDLService.GetBedPatientInfo().map(res => res);
  }
  //sud:16Sep'21
  GetBedOccupancyOfWards() {
    return this.helpdeskDLService.GetBedOccupancyOfWards().map(res => res);
  }

  //sud:16Sep'21
  GetAllBedsWithPatInfo() {
    return this.helpdeskDLService.GetAllBedsWithPatInfo().map(res => res);
  }
  public GetAppointmentData(deptId: number, doctorIds: number[], pendingOnly: boolean) {
    return this.helpdeskDLService.GetAppointmentData(deptId, doctorIds, pendingOnly).map((res) => {
      return res;
    })
  }
  public GetAllApptDepartment() {
    return this.helpdeskDLService.GetAllApptDepartment().map((res) => {
      return res;
    });
  }

  public GetAllAppointmentApplicableDoctor() {
    return this.helpdeskDLService.GetAllAppointmentApplicableDoctor().map((res) => {
      return res;
    });
  }
}
