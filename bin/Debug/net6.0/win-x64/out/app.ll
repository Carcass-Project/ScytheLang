; ModuleID = 'app'
source_filename = "app"
target datalayout = "e-m:w-p270:32:32-p271:32:32-p272:64:64-i64:64-f80:128-n8:16:32:64-S128"
target triple = "x86_64-pc-windows-msvc"

@strtmp = private unnamed_addr constant [6 x i8] c"HELLO\00", align 1

define i32 @fib(i32 %x) {
entry:
  %ifcond = icmp slt i32 %x, 2
  br i1 %ifcond, label %then, label %ifcont

then:                                             ; preds = %entry
  br label %ifcont

else:                                             ; No predecessors!
  br label %ifcont

ifcont:                                           ; preds = %then, %else, %entry
  %SubTMP = sub i32 %x, 1
  %CallTMP = call i32 @fib(i32 %SubTMP)
  %SubTMP1 = sub i32 %x, 2
  %CallTMP2 = call i32 @fib(i32 %SubTMP1)
  %AddTMP = add i32 %CallTMP, %CallTMP2
  ret i32 %AddTMP
}

define i32 @main() {
entry:
  %CallTMP = call i32 (i8*, ...) @printf(i8* getelementptr inbounds ([6 x i8], [6 x i8]* @strtmp, i32 0, i32 0))
  ret i32 1
}

declare i32 @printf(i8*, ...)
