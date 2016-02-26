module Aggregates
open Events
open Domain
open System

type InProgressOrder = {
  PlacedOrder : PlacedOrder
  ServedDrinks : DrinksItem list
  ServedFoods : FoodItem list
  PreparedFoods : FoodItem list
}
with
    member this.NonServedDrinks =
      List.except this.ServedDrinks this.PlacedOrder.DrinksItems
    member this.NonServedFoods =
      List.except this.ServedFoods this.PlacedOrder.FoodItems
    member this.NonPreparedFoods =
      List.except this.PreparedFoods this.PlacedOrder.FoodItems
    member this.IsOrderServed =
      List.isEmpty this.NonServedFoods && List.isEmpty this.NonServedDrinks

type State =
  | ClosedTab
  | OpenedTab
  | PlacedOrder of PlacedOrder
  | OrderInProgress of InProgressOrder
  | OrderServed of PlacedOrder

let getState (ipo : InProgressOrder) =
  if ipo.IsOrderServed then
    OrderServed ipo.PlacedOrder
  else
    OrderInProgress ipo

let apply state event  =
  match state, event  with
  | ClosedTab, TabOpened -> OpenedTab
  | OpenedTab _, OrderPlaced placeOrder -> PlacedOrder placeOrder
  | PlacedOrder placedOrder, DrinksServed item ->
      match List.contains item placedOrder.DrinksItems with
      | true ->
          {
            PlacedOrder = placedOrder
            ServedDrinks = [item]
            ServedFoods = []
            PreparedFoods = []
          } |> getState
      | false -> PlacedOrder placedOrder
  | OrderInProgress ipo, DrinksServed item ->
      match List.contains item ipo.NonServedDrinks with
      | true ->
          {ipo with ServedDrinks = item :: ipo.ServedDrinks}
          |> getState
      | false -> OrderInProgress ipo
  | PlacedOrder placedOrder, FoodPrepared item ->
      match List.contains item placedOrder.FoodItems with
      | true ->
          {
            PlacedOrder = placedOrder
            ServedDrinks = []
            ServedFoods = []
            PreparedFoods = [item]
          } |> OrderInProgress
      | false -> PlacedOrder placedOrder
  | OrderInProgress ipo, FoodPrepared item ->
      match List.contains item ipo.NonPreparedFoods with
      | true ->
          {ipo with PreparedFoods = item :: ipo.PreparedFoods}
          |> OrderInProgress
      | false -> OrderInProgress ipo
  | OrderInProgress ipo, FoodServed item ->
      let isPrepared = List.contains item ipo.PreparedFoods
      let isNonServed = List.contains item ipo.NonServedFoods
      match isPrepared, isNonServed with
      | true,true ->
          {ipo with ServedFoods = item :: ipo.ServedFoods}
          |> getState
      | _ -> OrderInProgress ipo
  | OrderServed _, TabClosed -> ClosedTab
  | _ -> state