
using QuickBalance.API.CLS;
using QuickBalance.API.DTOs;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QuickBalance.API.DTOs.Respons_DTO
{
    public class SelectedSalesOrderResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public SelectedSalesOrderData Data { get; set; } = new();
    }

    public class SelectedSalesOrderData
    {
        public SalesOrderHeaderDto Header { get; set; } = new();
        public List<SalesOrderItemDto> Items { get; set; } = new();
        public List<AddInfoDto> AdditionalInformations { get; set; } = new();
    }

    // Reuses the properties representing CORDM columns.
        public class SalesOrderHeaderDto: ClsCordM
        {
            //public int ID { get; set; }
            //public string? STYPE { get; set; }
            //public int? ORDNO { get; set; }
            public DateTime? ORDDATE { get; set; }
            public int? INVNO { get; set; }
            public DateTime? INVDATE { get; set; }
            public string? BTYPE { get; set; }
            public int? CUSCODE { get; set; }
            public decimal? DISCP { get; set; }
            public decimal? DISCVAL { get; set; }
            public decimal? NTVALUE { get; set; }
            public decimal? TVALUE { get; set; }
            public decimal? TAX { get; set; }
            public decimal? COINADJ { get; set; }
            public decimal? AMOUNT { get; set; }
            public decimal? FREIGHT { get; set; }
            public int? SMCODE { get; set; }
            public bool? RETURNFLG { get; set; }
            public bool? BILLFLG { get; set; }
            public int? LOGCD { get; set; }
            public int? LOGBILL { get; set; }
            public bool? CLOSED { get; set; }
            public string? REMARKS { get; set; }
            public bool? INCLUSIVE { get; set; }
            public DateTime? DELYDATE { get; set; }
            public decimal? INVAMOUNT { get; set; }
            public decimal? ADVANCE { get; set; }
            public DateTime? CREATEDDT { get; set; }
            public decimal? CUSDUITY { get; set; }
            public decimal? CURCONRATE { get; set; }
            public int? CURCODE { get; set; }
            public string? SHIPADDRESS { get; set; }
            public int? GDNO { get; set; }
            public string? PARTYORDNO { get; set; }
            public bool? CANCELFLG { get; set; }
            public int? INFORMAT { get; set; }
            public int? PRCATCODE { get; set; }
            public string? INVNOTEXT { get; set; }
            public int? VSVRTYPE { get; set; }
            public string? VSDOCNO { get; set; }
            public int? VSORDCD { get; set; }
            public int? PRLISTCODE { get; set; }
            public int? SYC_VRNO { get; set; }
            public int? SYC_ORDNO { get; set; }
            public string? CLOSEREMARKS { get; set; }
            public string? TRANSFERRED { get; set; }

            public string? QTNF1 { get; set; }
            public string? QTNF2 { get; set; }
            public string? QTNF3 { get; set; }
            public string? QTNF4 { get; set; }
            public string? QTNF5 { get; set; }
            public string? QTNF6 { get; set; }
            public string? QTNF7 { get; set; }
            public string? QTNF8 { get; set; }
            public string? QTNF9 { get; set; }
            public string? QTNF10 { get; set; }
            public string? QTNF11 { get; set; }
            public string? QTNF12 { get; set; }
            public string? QTNF13 { get; set; }
            public string? QTNF14 { get; set; }
            public string? QTNF15 { get; set; }
            public string? QTNF16 { get; set; }
            public string? QTNF17 { get; set; }
            public string? QTNF18 { get; set; }
            public string? QTNF19 { get; set; }
            public string? QTNF20 { get; set; }

            public string? SOF1 { get; set; }
            public string? SOF2 { get; set; }
            public string? SOF3 { get; set; }
            public string? SOF4 { get; set; }
            public string? SOF5 { get; set; }
            public string? SOF6 { get; set; }
            public string? SOF7 { get; set; }
            public string? SOF8 { get; set; }
            public string? SOF9 { get; set; }
            public string? SOF10 { get; set; }
            public string? SOF11 { get; set; }
            public string? SOF12 { get; set; }
            public string? SOF13 { get; set; }
            public string? SOF14 { get; set; }
            public string? SOF15 { get; set; }
            public string? SOF16 { get; set; }
            public string? SOF17 { get; set; }
            public string? SOF18 { get; set; }
            public string? SOF19 { get; set; }
            public string? SOF20 { get; set; }

           public int? ULOGCD { get; set; }
            public DateTime? UDATE { get; set; }
            public int? APRVLABY { get; set; }
            public DateTime? APRVLADT { get; set; }
            public int? APRVLBBY { get; set; }
            public DateTime? APRVLBDT { get; set; }
            public string? APRVLST { get; set; }
            public int? APRVLRBY { get; set; }
            public DateTime? APRVLRDT { get; set; }
            public int? APRVLJBY { get; set; }
            public DateTime? APRVLJDT { get; set; }

            // Joined display fields
            public string? SMNAME { get; set; }
            public string? ACCNAME { get; set; }
            public string? SALNO { get; set; }
            public string? USERNAME { get; set; }
    }

    // Reuses the properties representing CORDT columns.
    public class SalesOrderItemDto : ClsCordT
    {
        // CORDT fields
        public int INVNO { get; set; }
        public DateTime? INVDATE { get; set; }
        public string? BTYPE { get; set; }
        public int CUSCODE { get; set; }

        public decimal? QTY { get; set; }
        public decimal? RATE { get; set; }
        public decimal? PRATE { get; set; }
        public decimal? DISC { get; set; }

        public int? TAXCODE { get; set; }
        public decimal? TAX { get; set; }

        public bool? BILLFLG { get; set; }
        public bool? RETURNFLG { get; set; }

        public decimal? MRP { get; set; }
        public decimal? ORGRATE { get; set; }

        public int? SMCODE { get; set; }
        public int? LOGCD { get; set; }

        public string? PRNAME { get; set; }
        public string? ALCODE { get; set; }
        public string? ALNAME { get; set; }

        public int? UNITCODE { get; set; }
        public int? GDWCODE { get; set; }

        public string? PRDESC { get; set; }
        public DateTime? CREATEDDT { get; set; }

        public decimal? CURCONRATE { get; set; }
        public int? CURCODE { get; set; }
        public decimal? ORGQTY { get; set; }

        public bool? CANCELFLG { get; set; }
        public bool? PRFLG { get; set; }

        public int? CONVTYPE { get; set; }
        public decimal? CONVFACT { get; set; }
        public decimal? CONVQTY { get; set; }
        public decimal? CONVRATE { get; set; }

        public string? LDISCTYPE { get; set; }
        public decimal? LDISCPERC { get; set; }
        public decimal? LDISCAMT { get; set; }
        public decimal? DISCP { get; set; }

        public int? QTNCODE { get; set; }

        public decimal? BML { get; set; }
        public decimal? BMQ { get; set; }
        public decimal? BMW { get; set; }

        public int? VSVRTYPE { get; set; }

        public string? PRV { get; set; }
        public bool? CLOSES { get; set; }

        public int? ORDROWID { get; set; }
        public int? QTNROWID { get; set; }

        public string? PRVY { get; set; }

        // PRMAST product details
        public string? ALIAS { get; set; }
        public string? SECNAME { get; set; }
        public string? UNIT { get; set; }
        public string? ALTUNIT { get; set; }
        public string? SALESPRICETYPE { get; set; }
        public string? BMEQN { get; set; }

        // PRUNITMAS unit details
        public string? PRUNITNAME { get; set; }
        public string? UNITSECNAME { get; set; }

        // Internal ordering field; excluded from the API response.
        [JsonIgnore]
        public long SL { get; set; }
}

    public class AddInfoDto
    {
        public string? CODE { get; set; }
        public decimal? TAX { get; set; }
        public decimal? TAXAMT { get; set; }
    }
}
