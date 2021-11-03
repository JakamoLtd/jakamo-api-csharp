using System.IO;

namespace Jakamo.Api.DTO {

    /// <summary>
    /// This is the Data Transfer Object that contains SalesOrder related data
    /// </summary>
    public class SalesOrderDto
    {
        /// <summary>
        /// The Sales Order in an XML stream
        /// </summary>
        public Stream XmlStream { get; set; }
        
        /// <summary>
        /// The Acknowledgement URI. This is the endpoint to which a consumer *must* post
        /// when the consumer has processed the sales order successfully. What "successfully"
        /// means is up to the consumer.
        /// </summary>
        public string AcknowledgementUri { get; set; }
        
        /// <summary>
        /// The Confirmation URI. This is the endpoint to which a consumer *may* send
        /// an empty HTTP POST request, indicating that the consumer approves any and
        /// all order changes without an explicit OrderConfirmation XML message
        /// </summary>
        public string ConfirmationUri { get; set; }
        
        /// <summary>
        /// The seller-provided Order Number
        /// </summary>
        public string OrderNumber { get; set; }
    }
}