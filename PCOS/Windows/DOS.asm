; ============================================================================
; WINDOWS NT KERNEL CORE (x86-64 Assembly)
; ============================================================================
; This represents a simplified Windows NT kernel (ntoskrnl.exe)
; - Executive layer (object manager, I/O manager, etc.)
; - Kernel layer (scheduling, synchronization)
; - HAL (Hardware Abstraction Layer)
; ============================================================================

[BITS 64]
[ORG 0xFFFFF80000000000]  ; Windows kernel virtual address space

section .text
global _start
global KiSystemStartup
global KiDispatchInterrupt
global KiTrapFrame
global MmAccessFault

; ============================================================================
; WINDOWS NT CONSTANTS AND STRUCTURES
; ============================================================================

; Processor modes
KERNEL_MODE    equ 0
USER_MODE      equ 1

; Thread states
Initialized    equ 0
Ready          equ 1
Running        equ 2
Standby        equ 3
Terminated     equ 4
Waiting        equ 5

; Priority levels
LOW_REALTIME_PRIORITY   equ 16
HIGH_PRIORITY           equ 15
ABOVE_NORMAL_PRIORITY   equ 10
NORMAL_PRIORITY         equ 8
BELOW_NORMAL_PRIORITY   equ 6
IDLE_PRIORITY           equ 4

; ============================================================================
; KPROCESS STRUCTURE (Process Control Block)
; ============================================================================

struc KPROCESS
    .Header:            resb 24   ; DISPATCHER_HEADER
    .ProfileListHead:   resq 2    ; LIST_ENTRY
    .DirectoryTableBase: resq 2   ; Page directory
    .LdtDescriptor:     resq 2    ; KGDTENTRY64
    .Int21Descriptor:   resq 2    ; KIDTENTRY64
    .IopmOffset:        resw 1    ; WORD
    .ActiveProcessors:  resq 1    ; KAFFINITY_EX
    .KernelTime:        resd 1    ; DWORD
    .UserTime:          resd 1    ; DWORD
    .ReadyListHead:     resq 2    ; LIST_ENTRY
    .SwapListEntry:     resq 2    ; SINGLE_LIST_ENTRY
    .VdmTrapcHandler:   resq 1    ; PVOID
    .ThreadListHead:    resq 2    ; LIST_ENTRY
    .ProcessLock:       resd 1    ; KSPIN_LOCK
    .Affinity:          resq 1    ; KAFFINITY_EX
    .AutoAlignment:     resb 1    ; BOOLEAN
    .DisableBoost:      resb 1    ; BOOLEAN
    .DisableQuantum:    resb 1    ; BOOLEAN
    .Flags:             resb 1    ; UCHAR
    .BasePriority:      resb 1    ; CHAR
    .QuantumReset:      resb 1    ; CHAR
    .State:             resb 1    ; UCHAR
    .StackCount:        resb 1    ; UCHAR
    .ProcessListEntry:  resq 2    ; LIST_ENTRY
    .CycleTime:         resq 1    ; ULONGLONG
    .size:
endstruc

; ============================================================================
; KTHREAD STRUCTURE (Thread Control Block)
; ============================================================================

struc KTHREAD
    .Header:            resb 24   ; DISPATCHER_HEADER
    .SListFaultAddress: resq 1    ; PVOID
    .Quantum:           resd 1    ; ULONG
    .KernelStack:       resq 1    ; PVOID
    .Teb:               resq 1    ; PTEB
    .InitialStack:      resq 1    ; PVOID
    .StackLimit:        resq 1    ; PVOID
    .KernelStackResident: resb 1  ; BOOLEAN
    .AdjustReason:      resb 1    ; CHAR
    .AdjustIncrement:   resb 1    ; CHAR
    .SystemCallNumber:  resd 1    ; DWORD
    .FirstArgument:     resq 1    ; PVOID
    .TrapFrame:         resq 1    ; PKTRAP_FRAME
    .ApcState:          resb 64   ; KAPC_STATE
    .WaitStatus:        resd 1    ; LONG
    .WaitBlockList:     resq 1    ; PKWAIT_BLOCK
    .WaitListEntry:     resq 2    ; LIST_ENTRY
    .Queue:             resq 1    ; PRKQUEUE
    .ApcQueueLock:      resd 1    ; KSPIN_LOCK
    .ContextSwitches:   resd 1    ; ULONG
    .State:             resb 1    ; UCHAR
    .NpxState:          resb 1    ; UCHAR
    .WaitIrql:          resb 1    ; KIRQL
    .WaitMode:          resb 1    ; KPROCESSOR_MODE
    .WaitReason:        resb 1    ; KWAIT_REASON
    .Alertable:         resb 1    ; BOOLEAN
    .WaitNext:          resb 1    ; BOOLEAN
    .WaitTime:          resd 1    ; ULONG
    .KernelApcDisable:  resd 1    ; ULONG
    .UserAffinity:      resq 1    ; KAFFINITY
    .SystemAffinityActive: resb 1 ; BOOLEAN
    .MiscFlags:         resb 1    ; UCHAR
    .size:
endstruc

; ============================================================================
; KERNEL ENTRY POINT (NTOSKRNL)
; ============================================================================

_start:
    ; Windows kernel entry point
    mov rdi, rcx          ; Save loader parameters
    mov rsi, rdx
    mov rdx, r8
    mov rcx, r9
    
    ; Initialize basic CPU state
    call KiInitializeKernel
    
    ; Initialize HAL
    call HalInitializeProcessor
    
    ; Initialize executive
    call ExpInitializeExecutive
    
    ; Initialize kernel
    call KiInitializeKernel
    
    ; Initialize memory manager
    call MmInitSystem
    
    ; Initialize object manager
    call ObInitSystem
    
    ; Initialize process manager
    call PsInitSystem
    
    ; Initialize I/O manager
    call IoInitSystem
    
    ; Start system
    call KiSystemStartup
    
    ; Should never return
    cli
    hlt

KiSystemStartup:
    push rbp
    mov rbp, rsp
    
    ; Initialize interrupt controller
    call HalpInitializePICs
    
    ; Initialize timers
    call KeInitializeTimer
    
    ; Initialize DPCs
    call KeInitializeDpc
    
    ; Create System process
    call PsCreateSystemProcess
    
    ; Create idle thread
    call KiCreateIdleThread
    
    ; Initialize scheduler
    call KiInitializeScheduler
    
    ; Start idle loop
    call KiIdleLoop
    
    pop rbp
    ret

; ============================================================================
; DISPATCHER AND SCHEDULER
; ============================================================================

KiDispatchInterrupt:
    ; Save context
    push r15
    push r14
    push r13
    push r12
    push r11
    push r10
    push r9
    push r8
    push rdi
    push rsi
    push rbp
    push rbx
    push rdx
    push rcx
    push rax
    
    ; Get current thread
    mov rcx, gs:[0x188]      ; KPCR->Prcb->CurrentThread
    mov rbx, rcx             ; Save current thread
    
    ; Check for pending APCs
    call KiCheckForApcDelivery
    
    ; Check quantum expiration
    call KiQuantumEnd
    
    ; Select next thread
    call KiSelectNextThread
    mov r12, rax             ; New thread
    
    ; Compare with current thread
    cmp r12, rbx
    je .no_context_switch
    
    ; Context switch required
    mov rdi, rbx             ; Old thread
    mov rsi, r12             ; New thread
    call KiSwapContext

.no_context_switch:
    ; Restore context
    pop rax
    pop rcx
    pop rdx
    pop rbx
    pop rbp
    pop rsi
    pop rdi
    pop r8
    pop r9
    pop r10
    pop r11
    pop r12
    pop r13
    pop r14
    pop r15
    
    ; Return from interrupt
    iretq

; Context switching
KiSwapContext:
    push rbp
    mov rbp, rsp
    
    ; Save old thread context
    ; Save non-volatile registers
    mov [rdi + KTHREAD.KernelStack], rsp
    
    ; Save floating point state if needed
    call KiSaveProcessorControlState
    
    ; Switch to new thread
    mov rsp, [rsi + KTHREAD.KernelStack]
    
    ; Update current thread pointer
    mov rax, gs:[0x180]      ; KPCR->Prcb
    mov [rax + 0x8], rsi     ; Prcb->CurrentThread
    
    ; Restore floating point state if needed
    call KiRestoreProcessorControlState
    
    ; Set new CR3 if needed
    mov rax, [rsi + KTHREAD.ApcState + 0x20]  ; Thread->ApcState.Process
    mov rcx, [rax + KPROCESS.DirectoryTableBase]
    mov cr3, rcx
    
    pop rbp
    ret

; ============================================================================
; SYSTEM CALL HANDLING (SYSCALL/SYSENTER)
; ============================================================================

KiSystemCall64:
    ; Save user stack pointer
    swapgs
    mov gs:[0x10], rsp       ; KPCR->UserRsp
    mov rsp, gs:[0x1A8]      ; KPCR->KernelStack
    
    ; Build trap frame
    push 0x2b                ; SS
    push gs:[0x10]           ; User RSP
    push r11                 ; RFLAGS
    push 0x33                ; CS
    push rcx                 ; RIP (return address)
    
    ; Save general purpose registers
    push rbp
    mov rbp, rsp
    
    sub rsp, 20h
    mov [rbp - 0x8], rax
    mov [rbp - 0x10], rcx
    mov [rbp - 0x18], rdx
    mov [rbp - 0x20], r8
    mov [rbp - 0x28], r9
    mov [rbp - 0x30], r10
    mov [rbp - 0x38], r11
    
    ; Get system call number
    mov r10, rcx             ; Save return address
    mov eax, [r10 + 4]       ; System call number
    
    ; Validate system call
    cmp eax, KiServiceLimit
    jae .invalid_syscall
    
    ; Get service table
    mov r11, gs:[0x80]       ; KPCR->ServiceTable
    mov r10d, [r11 + rax*4]  ; Get service offset
    
    ; Call system service
    mov rcx, [rbp + 0x28]    ; First argument
    mov rdx, [rbp + 0x30]    ; Second argument
    mov r8, [rbp + 0x38]     ; Third argument
    mov r9, [rbp + 0x40]     ; Fourth argument
    
    call r10
    
    ; Save return value
    mov [rbp - 0x8], rax
    
    ; Return to user mode
    jmp KiSystemCallExit

.invalid_syscall:
    mov rax, 0xC0000000      ; STATUS_INVALID_SYSTEM_SERVICE
    mov [rbp - 0x8], rax
    jmp KiSystemCallExit

KiSystemCallExit:
    ; Restore registers
    mov rax, [rbp - 0x8]
    mov rcx, [rbp - 0x10]
    mov rdx, [rbp - 0x18]
    mov r8, [rbp - 0x20]
    mov r9, [rbp - 0x28]
    mov r10, [rbp - 0x30]
    mov r11, [rbp - 0x38]
    
    ; Restore stack
    mov rsp, rbp
    pop rbp
    
    ; Return to user mode
    swapgs
    sysretq

; ============================================================================
; MEMORY MANAGEMENT
; ============================================================================

MmAccessFault:
    ; rcx = ExceptionInfo
    push rbp
    mov rbp, rsp
    
    ; Get fault information
    mov rax, [rcx + 0x28]    ; ExceptionRecord->ExceptionInformation[0]
    mov rdx, [rcx + 0x30]    ; ExceptionRecord->ExceptionInformation[1]
    
    ; Check access type
    test rdx, 1              ; Write access?
    jnz .write_fault
    
    ; Read fault
    call MmAccessFaultRead
    jmp .fault_done

.write_fault:
    call MmAccessFaultWrite

.fault_done:
    ; Set return status
    mov rax, 0               ; STATUS_SUCCESS
    
    pop rbp
    ret

MmAccessFaultRead:
    ; Handle read page fault
    push rbp
    mov rbp, rsp
    
    ; Get current process
    mov rax, gs:[0x188]      ; CurrentThread
    mov rax, [rax + KTHREAD.ApcState + 0x20]  ; Thread->Process
    
    ; Look up virtual address
    mov rdi, rax             ; Process
    mov rsi, cr2             ; Fault address
    call MiResolveVirtualAddress
    
    test rax, rax
    jz .page_not_present
    
    ; Page is present but not accessible
    ; Check protection
    test byte [rax + 0x3], 1  ; Read access?
    jz .access_violation
    
    ; Make page accessible
    or byte [rax + 0x3], 1    ; Set present bit
    invlpg [rsi]             ; Invalidate TLB
    
    mov rax, 1               ; Success
    jmp .read_fault_done

.page_not_present:
    ; Page not present - allocate
    mov rdi, rsi             ; Virtual address
    mov rsi, 0x1000          ; Page size
    mov rdx, 0x20            ; Protection (PAGE_READONLY)
    call MiAllocateVirtualMemory
    
    test rax, rax
    jz .out_of_memory
    
    mov rax, 1               ; Success
    jmp .read_fault_done

.access_violation:
    mov rax, 0               ; Failure
    jmp .read_fault_done

.out_of_memory:
    mov rax, 0               ; Failure

.read_fault_done:
    pop rbp
    ret

; ============================================================================
; I/O MANAGER
; ============================================================================

IoCallDriver:
    ; rcx = DeviceObject, rdx = Irp
    push rbp
    mov rbp, rsp
    
    ; Get driver object
    mov rax, [rcx + 0x38]    ; DeviceObject->DriverObject
    mov rax, [rax + 0x70]    ; DriverObject->MajorFunction
    
    ; Get IRP stack location
    mov r8, [rdx + 0xB8]     ; Irp->Tail.Overlay.CurrentStackLocation
    movzx r9, byte [r8 + 0x43] ; StackLocation->MajorFunction
    
    ; Call appropriate driver routine
    mov rax, [rax + r9*8]
    call rax
    
    pop rbp
    ret

; ============================================================================
; OBJECT MANAGER
; ============================================================================

ObCreateObject:
    ; rcx = ObjectType, rdx = ObjectAttributes, r8 = ObjectSize
    push rbp
    mov rbp, rsp
    
    ; Allocate memory for object
    mov r9, r8               ; Size
    mov r8, 0x2000           ; PoolType = PagedPool
    call ExAllocatePoolWithTag
    
    test rax, rax
    jz .create_failed
    
    ; Initialize object header
    mov [rax + 0x0], rcx     ; ObjectType
    mov [rax + 0x8], 1       ; PointerCount = 1
    mov [rax + 0x10], 1      ; HandleCount = 1
    
    ; Copy object name if provided
    test rdx, rdx
    jz .create_done
    
    ; Get name from attributes
    mov rcx, [rdx + 0x10]    ; ObjectAttributes->ObjectName
    test rcx, rcx
    jz .create_done
    
    ; Copy name
    mov rsi, [rcx + 0x0]     ; UNICODE_STRING->Buffer
    mov rdi, rax
    add rdi, 0x30            ; Object name field
    mov rcx, [rcx + 0x8]     ; Length
    shr rcx, 1               ; Bytes to words
    rep movsw

.create_done:
    ; Return object
    mov rax, rax
    
.create_failed:
    pop rbp
    ret

; ============================================================================
; EXECUTIVE SUPPORT
; ============================================================================

ExAllocatePoolWithTag:
    ; r8 = PoolType, r9 = NumberOfBytes, r10 = Tag
    push rbp
    mov rbp, rsp
    
    ; Check pool type
    cmp r8, 0x0              ; NonPagedPool
    je .nonpaged_pool
    cmp r8, 0x2000           ; PagedPool
    je .paged_pool
    
    ; Invalid pool type
    xor rax, rax
    jmp .allocate_done

.nonpaged_pool:
    ; Allocate from non-paged pool
    mov rcx, r9
    call MmAllocateNonCachedMemory
    jmp .allocate_done

.paged_pool:
    ; Allocate from paged pool
    mov rcx, r9
    call MmAllocateCachedMemory

.allocate_done:
    ; Store tag if allocation successful
    test rax, rax
    jz .allocate_failed
    
    ; Store tag at beginning of allocation
    mov [rax - 0x8], r10d    ; Pool tag

.allocate_failed:
    pop rbp
    ret

; ============================================================================
; DATA SECTION
; ============================================================================

section .data
align 4096

; System call service table
KiServiceTable:
    dd NtCreateFile - KiServiceTable
    dd NtReadFile - KiServiceTable
    dd NtWriteFile - KiServiceTable
    dd NtCreateProcess - KiServiceTable
    dd NtTerminateProcess - KiServiceTable
KiServiceLimit equ ($ - KiServiceTable) / 4

; Process and thread lists
PsActiveProcessHead: dq 0
KiThreadListHead: dq 0

; Pool tags
'Dll ' equ 0x6C6C6444        ; "Dll"
'Thrd' equ 0x64726854        ; "Thrd"
'Proc' equ 0x636F7250        ; "Proc"

; ============================================================================
; STACK SECTION
; ============================================================================

section .bss
align 16

; Kernel stacks for each CPU
kernel_stacks:
    resb 16384 * 8           ; 8 CPUs, 16KB each

; Process and thread objects
SystemProcess: resb KPROCESS.size
IdleThread: resb KTHREAD.size

; Non-paged pool
NonPagedPool: resb 0x100000  ; 1MB non-paged pool

; Paged pool
PagedPool: resb 0x1000000    ; 16MB paged pool
