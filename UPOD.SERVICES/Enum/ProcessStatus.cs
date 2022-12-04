using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UPOD.SERVICES.Enum
{
    public enum ProcessStatus
    {
        PENDING,
        REJECTED,
        PREPARING,
        RESOLVING,
        RESOLVED,
        CANCELED,
        EDITING,
        COMPLETED,
        WARNING,
    }

    public enum ReportStatus
    {
        //1:có service hư, 2: không có service hư (nên có 2 status này)
        TROUBLED,
        STABILIZED,
        COMPLETED
    }
    public enum ScheduleStatus
    {
        SCHEDULED,
        WARNING,
        MISSED,
        PREPARING,
        MAINTAINING,
        COMPLETED,
        NOTIFIED,
    }
    public enum ObjectName
    {
        DE, //Device
        TE, //Technician
        CU, //Customer
        SE, //Service
        AG, //Agency
        RE, //Request
        AR, //Area
        CON, //Contract
        MR, //Maintenance report
        AD, //Admin
        MS, //Maintenance Schedule
        MRD, //Maintenance Report Device
        MRS, //Maintenance Report Service
    }
}
