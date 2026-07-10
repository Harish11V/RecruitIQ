export function mapBusinessError(errorCode: string | null | undefined): string {
  if (!errorCode) return 'An unexpected error occurred.';
  
  const code = errorCode.trim();

  switch (code) {
    case 'JobMustBeArchivedBeforeDelete':
      return 'A job must be archived before it can be deleted.';
    case 'JobAlreadyPublished':
      return 'This job has already been published.';
    case 'JobAlreadyArchived':
      return 'This job has already been archived.';
    case 'DepartmentNotFound':
      return 'The specified department was not found.';
    case 'HiringManagerNotFound':
      return 'The specified hiring manager was not found.';
    case 'JobNotFound':
      return 'This job no longer exists.';
    case 'ConcurrencyConflict':
      return 'This record has been modified by another user. Please refresh and try again.';
    case 'DepartmentAlreadyExists':
    case 'DepartmentNameAlreadyExists':
      return 'A department with this name already exists.';
    case 'DepartmentHasActiveJobs':
      return 'This department cannot be deleted because it is assigned to one or more jobs.';
    case 'InvalidCandidateStatusTransition':
      return 'The selected status transition is not allowed.';
    default:
      // Clean up CamelCase error code into a readable sentence
      return code.replace(/([A-Z])/g, ' $1').trim();
  }
}
