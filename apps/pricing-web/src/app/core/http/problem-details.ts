import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../../models/api.models';
export function messageFromProblem(problem: ProblemDetails | null | undefined): string {
  if (!problem) return 'The service is unavailable. Try again.';
  const errors = Array.isArray(problem.errors)
    ? problem.errors.map((x) => `${x.field}: ${x.message}`)
    : Object.entries(problem.errors ?? {}).flatMap(([k, v]) => v.map((x) => `${k}: ${x}`));
  return errors.length
    ? errors.join(' ')
    : problem.detail || problem.title || 'The request could not be completed.';
}
export function httpErrorMessage(error: unknown): string {
  return error instanceof HttpErrorResponse
    ? messageFromProblem(error.error as ProblemDetails)
    : 'The request could not be completed.';
}
