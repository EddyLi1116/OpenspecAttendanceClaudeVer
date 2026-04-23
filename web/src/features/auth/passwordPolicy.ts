export interface PasswordEvaluation {
  length: boolean;
  upper: boolean;
  lower: boolean;
  digit: boolean;
  symbol: boolean;
  ok: boolean;
}

const symbolRegex = /[!@#$%^&*?\-_+=()\[\]{}|:;,.<>/~]/;

export const evaluatePassword = (pwd: string): PasswordEvaluation => {
  const length = pwd.length >= 10;
  const upper = /[A-Z]/.test(pwd);
  const lower = /[a-z]/.test(pwd);
  const digit = /[0-9]/.test(pwd);
  const symbol = symbolRegex.test(pwd);
  return { length, upper, lower, digit, symbol, ok: length && upper && lower && digit && symbol };
};
