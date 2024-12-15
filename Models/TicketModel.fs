namespace Models.TicketModel
open System
type public TicketModel(id: Guid,seat: string,showtime: string,customerName: string) = 
    member this.id = id
    member this.seat = seat
    member this.showtime = showtime
    member this.customerName = customerName
