open System
open System.Windows.Forms
open Models.Seat
open System.IO
open System.Collections.Generic
open System.Text.RegularExpressions
open Models.TicketModel
open System.Text.Json
open System.Drawing

[<EntryPoint>]
let main _ =
    let filePath = @"C:/Users/Abdo/source/repos/TicketBookApp/tickets.json"
    if not (File.Exists filePath) then
        File.WriteAllText(filePath, "[]") // Initialize with an empty JSON array

    let loadTickets () =
        let json = File.ReadAllText(filePath)
        JsonSerializer.Deserialize<List<TicketModel>>(json) :?> List<TicketModel>

    let saveTickets (tickets: List<TicketModel>) =
        let options = JsonSerializerOptions(WriteIndented = true)
        let json = JsonSerializer.Serialize(tickets, options)
        File.WriteAllText(filePath, json)

    // Create a new form
    let form = new Form(Text = "Ticket Booking System", Width = 800, Height = 600)
    form.BackColor <- Color.FromArgb(245, 245, 245) // Light gray background
    form.Font <- new Font("Segoe UI", 10.0f)

    let number_of_col = 5
    let number_of_rows = 5
    let showtimeSeats = Dictionary<string, SeatModel list list>()
    let create2DSeats rows cols =
        [ for row in 1 .. rows -> [ for col in 1 .. cols -> SeatModel(row, col, false) ] ]

    // Adding some showtimes
    showtimeSeats.Add("10:00 AM", create2DSeats number_of_rows number_of_col)
    showtimeSeats.Add("11:00 AM", create2DSeats 3 4)
    let lblTitle = new Label(Text = "Ticket Booking System", Font = new Font("Segoe UI", 16.0f, FontStyle.Bold), AutoSize = true, Top = 20, Left = 50)
    let comboBoxForShowTime = new ComboBox(DropDownStyle = ComboBoxStyle.DropDownList, Top = 70, Left = 50, Width = 200)
    let panelSeats = new Panel(Top = 150, Left = 50, Width = 400, Height = 300, BorderStyle = BorderStyle.FixedSingle)
    let textBoxName = new TextBox(Top = 70, Left = 300, Width = 200, PlaceholderText = "Enter Your Name")
    let btnBook = new Button(Text = "BOOK", Top = 470, Left = 300, Width = 100, Height = 50)
    let lblStatus = new Label(Text = "", AutoSize = true, Top = 540, Left = 50, ForeColor = Color.DarkGreen)

    let updateSeatsFromTickets (selectedShowtime: string) =
        let tickets = 
            loadTickets() 
            |> Seq.toList // Convert the .NET List<TicketModel> to an F# list
        tickets 
        |> List.filter (fun (ticket: TicketModel) -> ticket.showtime.Trim() = selectedShowtime.Trim())
        |> List.iter (fun (ticket: TicketModel) ->
            let regex = Regex(@"R(\d+)C(\d+)")
            let matchResult = regex.Match(ticket.seat)
            if matchResult.Success then
                let row = int(matchResult.Groups.[1].Value)
                let col = int(matchResult.Groups.[2].Value)
                let seats = showtimeSeats.[selectedShowtime]
                let updatedSeats =
                    seats |> List.mapi (fun rIdx rowList ->
                        if rIdx = row - 1 then
                            rowList |> List.mapi (fun cIdx seat ->
                                if cIdx = col - 1 then SeatModel(seat.row, seat.col, true) else seat
                            )
                        else rowList
                    )
                showtimeSeats.[selectedShowtime] <- updatedSeats
        )



    let createSeatGrid seats =
        panelSeats.Controls.Clear()
        seats |> List.iteri (fun rowIndex row ->
            row |> List.iteri (fun colIndex (seat: SeatModel) ->
                let seatButton = new Button(Text = $"R{seat.row}C{seat.col}", Width = 40, Height = 40, Left = colIndex * 50, Top = rowIndex * 50)
                seatButton.BackColor <- if seat.flag then Color.Red else Color.LightGreen
                seatButton.Click.Add(fun _ ->
                    if not seat.flag then
                        seatButton.BackColor <- Color.Yellow // Selected for booking
                    )
                panelSeats.Controls.Add(seatButton)
            )
        )

    
    let keysList = showtimeSeats.Keys |> Seq.toList
    keysList |> List.iter (fun key -> comboBoxForShowTime.Items.Add(key) |> ignore)

    comboBoxForShowTime.SelectedIndexChanged.Add(fun _ ->
        let selectedShowtime = comboBoxForShowTime.SelectedItem.ToString()
        updateSeatsFromTickets(selectedShowtime) // Update seats from tickets
        createSeatGrid(showtimeSeats.[selectedShowtime])
    )

    btnBook.Click.Add(fun _ ->
        let selectedShowtime = comboBoxForShowTime.SelectedItem
        if isNull selectedShowtime then
            lblStatus.Text <- "Please select a showtime."
        elif String.IsNullOrWhiteSpace(textBoxName.Text) then
            lblStatus.Text <- "Please enter your name."
        else
            let selectedSeats =
                panelSeats.Controls
                |> Seq.cast<Button>
                |> Seq.filter (fun btn -> btn.BackColor = Color.Yellow)
                |> Seq.toList
            if selectedSeats.IsEmpty then
                lblStatus.Text <- "Please select at least one seat."
            else
                let mutable bookingSuccess = true
                selectedSeats |> List.iter (fun btn ->
                    let seatString = btn.Text
                    let regex = Regex(@"R(\d+)C(\d+)")
                    let mm = regex.Match(seatString)
                    if mm.Success then
                        let row = int(mm.Groups.[1].Value)
                        let col = int(mm.Groups.[2].Value)
                        let seats = showtimeSeats.[selectedShowtime.ToString()]
                        let targetSeat = seats.[row - 1].[col - 1]

                        if targetSeat.flag then
                            lblStatus.Text <- $"Seat {seatString} is already booked. Please choose another seat."
                            btn.BackColor <- Color.Red // Mark as booked
                            bookingSuccess <- false
                        else
                            let updatedSeats =
                                seats |> List.mapi (fun rIdx rowList ->
                                    if rIdx = row - 1 then
                                        rowList |> List.mapi (fun cIdx seat ->
                                            if cIdx = col - 1 then SeatModel(seat.row, seat.col, true) else seat
                                        )
                                    else rowList
                                )
                            showtimeSeats.[selectedShowtime.ToString()] <- updatedSeats
                            btn.BackColor <- Color.Red // Mark as booked
                            let ticket = TicketModel(Guid.NewGuid(), $"R{row}C{col}", selectedShowtime.ToString(), textBoxName.Text)
                            let tickets = loadTickets()
                            tickets.Add(ticket)
                            saveTickets tickets
                )
                if bookingSuccess then
                    lblStatus.Text <- "Booking successful!"
        )


    form.Controls.Add(lblTitle)
    form.Controls.Add(comboBoxForShowTime)
    form.Controls.Add(textBoxName)
    form.Controls.Add(panelSeats)
    form.Controls.Add(btnBook)
    form.Controls.Add(lblStatus)

    Application.Run(form)
    0
