// app.gleam - Main application interface
import physics
import gleam/io
import gleam/list
import gleam/float

// Define a public type for the application state
pub type AppState {
  AppState(
    interfaces: List(physics.PhysicsInterface),
    history: List(String),
    active_interface: Option(physics.PhysicsInterface)
  )
}

// Define a public type for user commands
pub type Command {
  CreateInterface(name: String, version: String)
  UseInterface(name: String, system: physics.PhysicsSystem, inputs: List(Float))
  ShowHistory
  ClearHistory
  Exit
  Help
}

// Public functions for the application interface

/// Create initial application state
pub fn create_app() -> AppState {
  AppState(
    interfaces: [],
    history: [],
    active_interface: None
  )
}

/// Process a command and return updated state
pub fn process_command(state: AppState, command: Command) -> AppState {
  case command {
    CreateInterface(name, version) ->
      create_new_interface(state, name, version)
    
    UseInterface(name, system, inputs) ->
      use_interface_command(state, name, system, inputs)
    
    ShowHistory ->
      show_history(state)
    
    ClearHistory ->
      clear_history(state)
    
    Exit ->
      exit_application(state)
    
    Help ->
      show_help(state)
  }
}

/// Create a new interface and add it to state
fn create_new_interface(state: AppState, name: String, version: String) -> AppState {
  let new_interface = physics.create_interface(
    name: name,
    version: version,
    calculate_fn: physics.example_calculator,
    validate_fn: physics.example_validator
  )
  
  let new_interfaces = list.append(state.interfaces, [new_interface])
  let history_entry = "Created interface: " <> name
  let new_history = list.append(state.history, [history_entry])
  
  AppState(
    interfaces: new_interfaces,
    history: new_history,
    active_interface: Some(new_interface)
  )
}

/// Use an interface by name
fn use_interface_command(
  state: AppState,
  name: String,
  system: physics.PhysicsSystem,
  inputs: List(Float)
) -> AppState {
  case find_interface(state.interfaces, name) {
    Some(interface) ->
      let result = physics.use_interface(interface, system, inputs)
      let is_valid = physics.validate_result(interface, result)
      
      let result_str = format_result(result, is_valid)
      let history_entry = name <> ": " <> result_str
      let new_history = list.append(state.history, [history_entry])
      
      io.println(result_str)
      
      AppState(
        interfaces: state.interfaces,
        history: new_history,
        active_interface: Some(interface)
      )
    
    None ->
      io.println("Interface not found: " <> name)
      state
  }
}

/// Show command history
fn show_history(state: AppState) -> AppState {
  io.println("=== Command History ===")
  list.each(state.history, io.println)
  io.println("======================")
  state
}

/// Clear command history
fn clear_history(state: AppState) -> AppState {
  io.println("History cleared")
  AppState(
    interfaces: state.interfaces,
    history: [],
    active_interface: state.active_interface
  )
}

/// Exit the application
fn exit_application(state: AppState) -> AppState {
  io.println("Exiting application...")
  state  // In a real app, you'd handle exit differently
}

/// Show help information
fn show_help(state: AppState) -> AppState {
  io.println("=== Physics Calculator Help ===")
  io.println("Available commands:")
  io.println("  create <name> <version> - Create new interface")
  io.println("  use <name> <system> <inputs> - Use interface")
  io.println("  history - Show command history")
  io.println("  clear - Clear history")
  io.println("  help - Show this help")
  io.println("  exit - Exit application")
  io.println("")
  io.println("Available systems:")
  io.println("  Classical, Quantum, Relativistic, Thermodynamic")
  io.println("================================")
  state
}

/// Find interface by name
fn find_interface(
  interfaces: List(physics.PhysicsInterface),
  name: String
) -> Option(physics.PhysicsInterface) {
  list.find(interfaces, fn(interface) { interface.name == name })
}

/// Format a calculation result
fn format_result(result: physics.CalculationResult, is_valid: Bool) -> String {
  case result {
    physics.Result(value, unit, system) ->
      "Result: " <> float.to_string(value) <> " " <> unit <>
      " (" <> system_to_string(system) <> ")" <>
      " [Valid: " <> bool.to_string(is_valid) <> "]"
    
    physics.Error(message) ->
      "Error: " <> message
  }
}

/// Convert PhysicsSystem to string
fn system_to_string(system: physics.PhysicsSystem) -> String {
  case system {
    physics.Classical -> "Classical"
    physics.Quantum -> "Quantum"
    physics.Relativistic -> "Relativistic"
    physics.Thermodynamic -> "Thermodynamic"
  }
}

// Example usage functions

/// Run a simple example calculation
pub fn run_example() {
  let coord1 = physics.create_coordinate(0.0, 0.0)
  let coord2 = physics.create_coordinate(3.0, 4.0)
  let dist = physics.distance(coord1, coord2)
  
  io.println("Distance between coordinates: " <> float.to_string(dist))
  
  let vec1 = physics.create_vector(1.0, 2.0, 3.0)
  let vec2 = physics.create_vector(4.0, 5.0, 6.0)
  let sum = physics.add_vectors(vec1, vec2)
  
  io.println("Vector sum: (" <>
    float.to_string(sum.x) <> ", " <>
    float.to_string(sum.y) <> ", " <>
    float.to_string(sum.z) <> ")")
}

/// Create and use a physics interface
pub fn run_interface_example() {
  io.println("=== Physics Interface Example ===")
  
  // Create an interface
  let my_interface = physics.create_interface(
    name: "Basic Physics Calculator",
    version: "1.0.0",
    calculate_fn: physics.example_calculator,
    validate_fn: physics.example_validator
  )
  
  // Show interface info
  io.println(physics.get_interface_info(my_interface))
  
  // Use the interface
  let result1 = physics.use_interface(
    my_interface,
    physics.Classical,
    [10.0, 5.0]  // mass=10kg, velocity=5m/s
  )
  
  let valid1 = physics.validate_result(my_interface, result1)
  io.println(format_result(result1, valid1))
  
  let result2 = physics.use_interface(
    my_interface,
    physics.Relativistic,
    [1.0]  // mass=1kg
  )
  
  let valid2 = physics.validate_result(my_interface, result2)
  io.println(format_result(result2, valid2))
  
  io.println("=================================")
}

// Main entry point
pub fn main() {
  io.println("Welcome to the Physics Calculator!")
  
  // Run examples
  run_example()
  io.println("")
  run_interface_example()
  
  // Create and run the application
  let app = create_app()
  
  // Process some commands
  let app1 = process_command(app, Help)
  let app2 = process_command(app1, CreateInterface("MyCalc", "2.0"))
  
  let _ = process_command(app2, UseInterface(
    "MyCalc",
    physics.Classical,
    [5.0, 10.0]  // mass=5kg, velocity=10m/s
  ))
  
  let _ = process_command(app2, ShowHistory)
  let _ = process_command(app2, Exit)
  
  io.println("Application finished.")
}
