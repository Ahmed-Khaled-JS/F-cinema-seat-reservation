open System
open System.Windows.Forms
open Models.Seat
open System.IO
open System.Collections.Generic
open System.Text.RegularExpressions
open Models.TicketModel
open System.Text.Json

[<EntryPoint>]
let main _ =
    let filePath = @"C:/Users/Abdo/source/repos/TicketBookApp/tickets.json"
    if not (File.Exists filePath) then
        File.WriteAllText(filePath, "[]") // Initialize with an empty JSON array
    let loadTickets () =
        let json = File.ReadAllText(filePath)
        JsonSerializer.Deserialize<List<TicketModel>>(json) :?> List<TicketModel>
    // Function to save tickets to the JSON file
    let saveTickets (tickets: List<TicketModel>) =
        let options = JsonSerializerOptions(WriteIndented = true)
        let json = JsonSerializer.Serialize(tickets, options)
        File.WriteAllText(filePath, json)

    // Create a new form
    let form = new Form(Text = "F# Windows Forms Example", Width = 600, Height = 600)
    let seat = SeatModel(1 , 1, false)
    let number_of_col = 5
    let number_of_rows = 5
    let showtimeSeats = Dictionary<string, SeatModel list list>()

    let create2DSeats rows cols =
        [ for row in 1 .. rows ->
            [ for col in 1 .. cols -> SeatModel(row, col, false) ] ]
    
    
    showtimeSeats.Add("10:00 AM" , create2DSeats number_of_rows number_of_col)
    showtimeSeats.Add("11:00 AM" , create2DSeats 3 4)
        
    let getAvailableSeats (seats: SeatModel list list) =
        seats
        |> List.collect id // Flatten the 2D list into a single list
        |> List.filter (fun seat -> not seat.flag) // Filter for available seats
    
    let comboBoxForShowTime = new ComboBox(DropDownStyle = ComboBoxStyle.DropDownList, Top = 50, Left = 100, Width = 200)
    let comboBox = new ComboBox(DropDownStyle = ComboBoxStyle.DropDownList, Top = 100, Left = 100, Width = 200)
    let button = new Button(Text = "BOOK", Top = 250, Left = 100, Width = 100, Height = 50)
    let keysList = showtimeSeats.Keys |> Seq.toList
    keysList |> List.iter (fun key -> comboBoxForShowTime.Items.Add(key) |> ignore)
    
    let extractSeatDetails (seatString: string) =
        let pattern = @"Seat\(Row: (\d+), Col: (\d+)\)"
        let regex = new Regex(pattern)
        let momo = regex.Match(seatString)
    
        if momo.Success then
            let row = int momo.Groups.[1].Value
            let col = int momo.Groups.[2].Value
            Some(row, col)
        else
            None

    
    comboBoxForShowTime.SelectedIndexChanged.Add(fun _ ->
        comboBox.Items.Clear()
        let availableSeats = getAvailableSeats showtimeSeats.[comboBoxForShowTime.SelectedItem.ToString()]
        availableSeats 
        |> List.iter (fun seat -> comboBox.Items.Add(seat) |> ignore)
    )
    let textBox = new TextBox(Top = 10, Left = 50, Width = 200)
    form.Controls.Add(textBox)
    form.Controls.Add(comboBoxForShowTime)
    form.Controls.Add(comboBox)
    form.Controls.Add(button)
    button.Click.Add(fun _ ->
    match extractSeatDetails (comboBox.SelectedItem.ToString()) with
    | Some(row, col) -> 
        let extractedRow = row
        let extractedCol = col

        // Get the showtime key from the comboBox
        let showtimeKey = comboBoxForShowTime.SelectedItem.ToString()

        // Access the seat list for the selected showtime
        let seatList = showtimeSeats.[showtimeKey]
        
        // Access the specific row and seat (remember, lists are 0-indexed)
        let rowList = seatList.[extractedRow - 1]
        let seat = rowList.[extractedCol - 1]

        // Create a new SeatModel with updated flag
        let updatedSeat = SeatModel(seat.row, seat.col, true)  // Set flag to true

        // Replace the seat in the row with the updated seat
        let updatedRowList = 
            rowList |> List.mapi (fun index s -> if index = extractedCol - 1 then updatedSeat else s)

        // Replace the old row with the updated row in the seat list
        let updatedSeatList = 
            seatList |> List.mapi (fun index r -> if index = extractedRow - 1 then updatedRowList else r)

        // Update the dictionary with the modified seat list
        showtimeSeats.[showtimeKey] <- updatedSeatList

        // Print updated seat info
        printfn "Row: %d, Col: %d, Flag: %b, Showtime Key: %s" extractedRow extractedCol updatedSeat.flag showtimeKey

        // Print all seats after the update
        showtimeSeats |> Seq.iter (fun kvp ->
            let showtime = kvp.Key
            let seatList = kvp.Value
            printfn "Showtime: %s" showtime
            seatList |> List.iter (fun row ->
                row |> List.iter (fun seat ->
                    printfn "Row: %d, Col: %d, Flag: %b" seat.row seat.col seat.flag
                )
            )
        )

    | None -> 
        printfn "Invalid seat string"

    // Clear the comboBox items and add available seats
    printfn "i am here"
    comboBox.Items.Clear()
    let availableSeats = getAvailableSeats showtimeSeats.[comboBoxForShowTime.SelectedItem.ToString()]
    availableSeats |> List.iter (fun seat -> comboBox.Items.Add(seat) |> ignore)
    

    let Ticket = TicketModel(Guid.NewGuid(),$"R{seat.row}C{seat.col}", comboBoxForShowTime.SelectedItem.ToString(), textBox.Text)
    let tickets = loadTickets()
    tickets.Add(Ticket)
    saveTickets tickets

    // Show message
    MessageBox.Show($"Button was clicked!{comboBox.SelectedItem}") |> ignore
    )


    

    // Run the application
    Application.Run(form)
    0 // Return an integer exit code
