namespace Models.Seat


type public SeatModel(row: int,col: int,flag :bool) = 
    
    member this.col = col
    member this.row = row
    member this.flag = flag
    override this.ToString() = $"Seat(Row: {row}, Col: {col})"

