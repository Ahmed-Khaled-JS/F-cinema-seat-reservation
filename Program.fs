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
    let filePath = @"D:\College\4th Year\PL3\JS PROJECT V1\TicketBookApp\TicketBookApp\tickets.json"
    if not (File.Exists filePath) then
        File.WriteAllText(filePath, "[]") // Initialize with an empty JSON array

    let loadTickets () =
        let json = File.ReadAllText(filePath)
        JsonSerializer.Deserialize<List<TicketModel>>(json) :?> List<TicketModel>

    let saveTickets (tickets: List<TicketModel>) =
        let options = JsonSerializerOptions(WriteIndented = true)
        let json = JsonSerializer.Serialize(tickets, options)
        File.WriteAllText(filePath, json)

    // Create the form
    let form = new Form(Text = "Ticket Booking System", Width = 850, Height = 650)
    form.BackColor <- Color.WhiteSmoke // Use a light, modern background color
    form.Font <- new Font("Segoe UI", 10.0f)

    let number_of_col = 5
    let number_of_rows = 5
    let showtimeSeats = Dictionary<string, SeatModel list list>()
    let create2DSeats rows cols =
        [ for row in 1 .. rows -> [ for col in 1 .. cols -> SeatModel(row, col, false) ] ]

    // Adding some showtimes
    showtimeSeats.Add("10:00 AM", create2DSeats number_of_rows number_of_col)
    showtimeSeats.Add("11:00 AM", create2DSeats 3 4)

    // Title
    let lblTitle = new Label(Text = "Ticket Booking System", Font = new Font("Segoe UI", 18.0f, FontStyle.Bold), AutoSize = true, Top = 20, Left = 300)

    // Labels for dropdown, name, and grid
    let lblShowtime = new Label(Text = "Select Showtime:", Top = 70, Left = 50, Width = 150)
    let comboBoxForShowTime = new ComboBox(DropDownStyle = ComboBoxStyle.DropDownList, Top = 95, Left = 50, Width = 200)

    let lblName = new Label(Text = "Enter Your Name:", Top = 70, Left = 300, Width = 200)
    let textBoxName = new TextBox(Top = 95, Left = 300, Width = 250, PlaceholderText = "Enter your name here")

    let lblSeatGrid = new Label(Text = "Available Seats:", Top = 140, Left = 50, Width = 200)
    let panelSeats = new Panel(Top = 170, Left = 50, Width = 500, Height = 300, BorderStyle = BorderStyle.FixedSingle)
    panelSeats.BackColor <- Color.White

    // Book button
    let btnBook = new Button(Text = "BOOK", Top = 500, Left = 350, Width = 150, Height = 50)
    btnBook.BackColor <- Color.FromArgb(0, 120, 215) // Attractive blue color
    btnBook.ForeColor <- Color.White
    btnBook.FlatStyle <- FlatStyle.Flat
    btnBook.FlatAppearance.BorderSize <- 0
    btnBook.Font <- new Font("Segoe UI", 12.0f, FontStyle.Bold)

    // Status label
    let lblStatus = new Label(Text = "", AutoSize = true, Top = 570, Left = 50, ForeColor = Color.DarkGreen, Font = new Font("Segoe UI", 10.0f, FontStyle.Bold))

    let updateSeatsFromTickets (selectedShowtime: string) =
        let tickets = 
            loadTickets() 
            |> Seq.toList
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
                seatButton.FlatStyle <- FlatStyle.Flat
                seatButton.FlatAppearance.BorderSize <- 0
                seatButton.Enabled <- not seat.flag // Disable the button if the seat is booked
                seatButton.Click.Add(fun _ ->
                    if not seat.flag then
                        seatButton.BackColor <- Color.Yellow
                    )
                panelSeats.Controls.Add(seatButton)
            )
        )

    let keysList = showtimeSeats.Keys |> Seq.toList
    keysList |> List.iter (fun key -> comboBoxForShowTime.Items.Add(key) |> ignore)

    comboBoxForShowTime.SelectedIndexChanged.Add(fun _ ->
        let selectedShowtime = comboBoxForShowTime.SelectedItem.ToString()
        updateSeatsFromTickets(selectedShowtime)
        createSeatGrid(showtimeSeats.[selectedShowtime])
    )

    btnBook.Click.Add(fun _ ->
        let selectedShowtime = comboBoxForShowTime.SelectedItem
        if isNull selectedShowtime then
            lblStatus.Text <- "Please select a showtime."
            lblStatus.ForeColor <- Color.Red
        elif String.IsNullOrWhiteSpace(textBoxName.Text) then
            lblStatus.Text <- "Please enter your name."
            lblStatus.ForeColor <- Color.Red
        else
            let selectedSeats =
                panelSeats.Controls
                |> Seq.cast<Button>
                |> Seq.filter (fun btn -> btn.BackColor = Color.Yellow)
                |> Seq.toList
            if selectedSeats.IsEmpty then
                lblStatus.Text <- "Please select at least one seat."
                lblStatus.ForeColor <- Color.Red
            else
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
                            lblStatus.Text <- $"Seat {seatString} is already booked."
                            lblStatus.ForeColor <- Color.Red
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
                            btn.BackColor <- Color.Red
                            btn.Enabled <- false // Disable the button once booked
                            let ticket = TicketModel(Guid.NewGuid(), $"R{row}C{col}", selectedShowtime.ToString(), textBoxName.Text)
                            let tickets = loadTickets()
                            tickets.Add(ticket)
                            saveTickets tickets
                            lblStatus.Text <- "Booking successful!"
                            lblStatus.ForeColor <- Color.Green
                )
        )

    // Add controls to the form
    form.Controls.Add(lblTitle)
    form.Controls.Add(lblShowtime)
    form.Controls.Add(comboBoxForShowTime)
    form.Controls.Add(lblName)
    form.Controls.Add(textBoxName)
    form.Controls.Add(lblSeatGrid)
    form.Controls.Add(panelSeats)
    form.Controls.Add(btnBook)
    form.Controls.Add(lblStatus)

    Application.Run(form)
    0
